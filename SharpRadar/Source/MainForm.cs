using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharpRadar
{
    public partial class MainForm : Form
    {
        private readonly Memory _memory; // Reference to memory module
        private readonly object _renderLock = new object();
        private readonly List<Map> _allMaps; // Contains all maps from \\Maps folder
        private int _mapIndex = 0;
        private Map _currentMap; // Current Selected Map
        private Bitmap _currentRender; // Currently rendered frame
        private float _zoom = 1.0f;
        private int _lastZoom = 0;
        private const int _maxZoom = 3500;
        private Player _currentPlayer = new Player( // Keep track of current player (for Map centering)
            "player",
            "0",
            PlayerType.CurrentPlayer) // Default Starting Values
        {
            Position = new UnityEngine.Vector3(0, 0, 0)
        };
        private bool _startup = false; // Game startup flag


        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm(Memory memory)
        {
            InitializeComponent();
            _memory = memory;
            _allMaps = new List<Map>();
            LoadMaps();
            this.DoubleBuffered = true; // Prevent flickering
            this.mapCanvas.Paint += mapCanvas_OnPaint;
            this.Resize += MainForm_Resize;
            this.Shown += MainForm_Shown;
        }

        /// <summary>
        /// Load map files (.PNG) and Configs (.JSON) from \\Maps folder.
        /// </summary>
        private void LoadMaps()
        {
            var maps = Directory.GetFiles("\\Maps", "*.png"); // Get all PNG Files
            if (maps.Length == 0) throw new IOException("Unable to load map files at \\Maps");
            foreach (var map in maps)
            {
                var name = Path.GetFileNameWithoutExtension(map);
                var config = name + ".json";
                if (!File.Exists(config)) throw new IOException($"Map JSON Config missing for {map}");
                _allMaps.Add(new Map
                (
                    name.ToUpper(),
                    new Bitmap(Image.FromFile(map)),
                    MapConfig.LoadFromFile(config))
                );
            }
            _currentMap = _allMaps[0];
            label_Map.Text = _currentMap.Name;
            _currentRender = (Bitmap)_currentMap.MapFile.Clone();
        }

        /// <summary>
        /// Main UI Loop.
        /// </summary>
        private async void MainForm_Shown(object sender, EventArgs e)
        {
            while (true)
            {
                refresh();
                await Task.Delay(33); // Render timer (~30 fps?)
            }
        }

        /// <summary>
        /// Handle window resizing
        /// </summary>
        private void MainForm_Resize(object sender, System.EventArgs e)
        {
            lock (_renderLock)
            {
                mapCanvas.Size = new Size(this.Height, this.Height); // Keep square aspect ratio
                mapCanvas.Location = new Point(0, 0); // Keep in top left corner
            }
        }

        /// <summary>
        /// GUI Refresh method
        /// </summary>
        private void refresh() // Request GUI to render next frame
        {
            if (!_startup && _memory.InGame)
            {
                foreach (KeyValuePair<string,Player> player in _memory.Players)
                {
                    lock (player.Value) // Obtain object lock
                    {
                        if (player.Value.Type is PlayerType.CurrentPlayer) // Determine current player
                        {
                            _currentPlayer = player.Value;
                            _startup = true;
                            break;
                        }
                    }
                }
            }
            if (_memory.InGame)
            {
                mapCanvas.Invalidate(); // Clears canvas, causing it to be re-drawn
            }
            else _startup = false;
        }

        /// <summary>
        /// Control handles map zoom
        /// </summary>
        private void trackBar_Zoom_Scroll(object sender, EventArgs e)
        {
            int amtChanged = trackBar_Zoom.Value - _lastZoom;
            _lastZoom = trackBar_Zoom.Value;
            _zoom -= (.01f) * (amtChanged);
        }

        /// <summary>
        /// Draw/Render on Map Canvas
        /// </summary>
        private void mapCanvas_OnPaint(object sender, PaintEventArgs e)
        {
            lock (_renderLock)
            {
                var render = GetRender(); // Construct next frame
                mapCanvas.Image = render; // Render next frame
                
                // Cleanup Resources
                _currentRender.Dispose(); // Dispose previous frame
                _currentRender = render; // Store reference of current frame
            }
        }

        /// <summary>
        /// Draws next render frame and returns a completed Bitmap
        /// </summary>
        private Bitmap GetRender()
        {
            int zoom = (int)(_maxZoom * _zoom); // Get zoom level
            int strokeLength = zoom / 125; // Lower constant = longer stroke
            if (strokeLength < 5) strokeLength = 5; // Min value
            int strokeWidth = zoom / 300; // Lower constant = wider stroke
            if (strokeWidth < 4) strokeWidth = 4; // Min value
            using (var render = (Bitmap)_currentMap.MapFile.Clone()) // Get a fresh map to draw on
            using (var grn = new Pen(Color.LimeGreen)
            {
                Width = strokeWidth
            })
            using (var red = new Pen(Color.Red)
            {
                Width = strokeWidth
            })
            using (var ylw = new Pen(Color.Yellow)
            {
                Width = strokeWidth
            })
            using (var vlt = new Pen(Color.Violet)
            {
                Width = strokeWidth
            })
            using (var wht = new Pen(Color.White)
            {
                Width = strokeWidth
            })
            using (var blk = new Pen(Color.Black)
            {
                Width = strokeWidth
            })
            {
                MapPosition playerPos;
                lock (_currentPlayer) // Obtain object lock
                {
                    playerPos = VectorToMapPos(_currentPlayer.Position);
                    label_Pos.Text = $"X: {_currentPlayer.Position.x}\r\nY: {_currentPlayer.Position.y}\r\nZ: {_currentPlayer.Position.z}";
                }
                // Get map frame bounds (Based on Zoom Level, centered on Current Player)
                var bounds = new Rectangle(playerPos.X - zoom / 2, playerPos.Y - zoom / 2, zoom, zoom);
                using (var gr = Graphics.FromImage(render)) // Get fresh frame
                {
                    // Draw Current Player
                    gr.DrawLine(grn, new Point(playerPos.X - strokeLength, playerPos.Y), new Point(playerPos.X + strokeLength, playerPos.Y));
                    gr.DrawLine(grn, new Point(playerPos.X, playerPos.Y - strokeLength), new Point(playerPos.X, playerPos.Y + strokeLength));
                    // Draw Other Players
                    foreach (KeyValuePair<string, Player> player in _memory.Players) // Draw PMCs
                    {
                        lock (player.Value) // Obtain object lock
                        {
                            if (player.Value.Type is PlayerType.CurrentPlayer) continue; // Already drawn current player, move on
                            var unitPos = VectorToMapPos(player.Value.Position);
                            if (unitPos.X >= bounds.Left // Only draw if in bounds
                                && unitPos.Y >= bounds.Top
                                && unitPos.X <= bounds.Right
                                && unitPos.Y <= bounds.Bottom)
                            { // Draw Location Marker
                                Pen pen;
                                if (player.Value.IsAlive is false)
                                {
                                    // Draw death marker (black 'X')
                                    continue;
                                }
                                else if (player.Value.Type is PlayerType.Teammate) pen = grn;
                                else if (player.Value.Type is PlayerType.PMC) pen = red;
                                else if (player.Value.Type is PlayerType.PlayerScav) pen = wht;
                                else if (player.Value.Type is PlayerType.AIBoss) pen = vlt;
                                else if (player.Value.Type is PlayerType.AIScav) pen = ylw;
                                else pen = red; // Default
                                gr.DrawLine(pen, new Point(unitPos.X - strokeLength, unitPos.Y), new Point(unitPos.X + strokeLength, unitPos.Y));
                                gr.DrawLine(pen, new Point(unitPos.X, unitPos.Y - strokeLength), new Point(unitPos.X, unitPos.Y + strokeLength));
                            }
                        }
                    }
                    /// ToDo - Handle Loot/Items
                }
                return CropImage(render, bounds); // Return the portion of the map to be rendered based on Zoom Level
            }
        }

        /// <summary>
        /// Returns a rectangular section of a source Bitmap
        /// </summary>
        private Bitmap CropImage(Bitmap source, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var gr = Graphics.FromImage(bitmap))
            {
                gr.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }

        /// <summary>
        /// ToDo - Convert game positional values to UI Map Coordinates.
        /// </summary>
        private MapPosition VectorToMapPos(UnityEngine.Vector3 vector)
        {
            return new MapPosition();
        }

        /// <summary>
        /// Toggle current map selection.
        /// </summary>
        private void button_Map_Click(object sender, EventArgs e)
        {
            if (_mapIndex == _allMaps.Count - 1) _mapIndex = 0; // Start over when end of maps reached
            else _mapIndex++; // Move onto next map
            lock (_renderLock) // Don't switch map mid-render
            {
                _currentMap = _allMaps[_mapIndex]; // Swap map
            }
            label_Map.Text = _currentMap.Name;
        }
    }
}

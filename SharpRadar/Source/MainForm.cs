using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharpRadar
{
    public partial class MainForm : Form
    {
        private readonly Memory _memory;
        private Bitmap _currentMap;
        private Bitmap _currentRender;
        private System.Timers.Timer _timer;
        private static Random _rng = new Random();
        private float _zoom = 1.0f;
        private int _lastZoom = 0;
        private const int _maxZoom = 3500;
        private Player _currentPlayer = new Player("0") // Default Starting Values
        {
            Position = new UnityEngine.Vector3(0, 0, 0)
        };
        private bool _startup = false;
        public MainForm(Memory memory)
        {
            InitializeComponent();
            _memory = memory;
            this.DoubleBuffered = true; // Prevent flickering
            this.Resize += this.MainForm_Resize;
            _currentMap = new Bitmap(Image.FromFile("Resources\\lighthouse.png")); // Load Map File
            _currentRender = (Bitmap)_currentMap.Clone();
            mapCanvas.Paint += this.mapCanvas_OnPaint;
            _timer = new System.Timers.Timer(33);
            _timer.Elapsed += this.tick;
            _timer.Start();
        }

        /// <summary>
        /// Handle window resizing
        /// </summary>
        private void MainForm_Resize(object sender, System.EventArgs e)
        {
            _timer.Reset();
            mapCanvas.Size = new Size(this.Height, this.Height); // Keep square aspect ratio
            mapCanvas.Location = new Point(0, 0); // Keep in top left corner
            refresh();
        }

        /// <summary>
        /// Render timer (~30 fps?)
        /// </summary>
        private void tick(Object source, System.Timers.ElapsedEventArgs e)
        {
            refresh();
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
                    if (player.Value.IsPlayer) // Determine current player
                    {
                        _currentPlayer = player.Value;
                        _startup = true;
                        break;
                    }
                }
            }
            while (_memory.InGame)
            {
                mapCanvas.Invalidate(); // Clears canvas, causing it to be re-drawn
                return;
            }
            _startup = false;
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
            var render = GetRender(); // Construct next frame
            mapCanvas.Image = render; // Render next frame

            // Cleanup Resources
            _currentRender.Dispose(); // Dispose previous frame
            _currentRender = render; // Store reference of current frame
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
            using (var render = (Bitmap)_currentMap.Clone()) // Get a fresh map to draw on
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
            {
                var playerPos = VectorToPositions(_currentPlayer.Position);
                // Get map frame bounds (Based on Zoom Level)
                var bounds = new Rectangle(playerPos.X - zoom / 2, playerPos.Y - zoom / 2, zoom, zoom);
                using (var gr = Graphics.FromImage(render)) // Get fresh frame
                {
                    // Draw Player
                    gr.DrawLine(grn, new Point(playerPos.X - strokeLength, playerPos.Y), new Point(playerPos.X + strokeLength, playerPos.Y));
                    gr.DrawLine(grn, new Point(playerPos.X, playerPos.Y - strokeLength), new Point(playerPos.X, playerPos.Y + strokeLength));
                    // Draw Units
                    foreach (KeyValuePair<string, Player> unit in _memory.Players) // Draw PMCs
                    {
                        if (unit.Value.IsPlayer) continue; // Already drawn current player, move on
                        // ToDo , add logic for scav/player scav/pmc/boss
                        var unitPos = VectorToPositions(unit.Value.Position);
                        if (unitPos.X >= bounds.Left // Only draw if in bounds
                            && unitPos.Y >= bounds.Top
                            && unitPos.X <= bounds.Right
                            && unitPos.Y <= bounds.Bottom)
                        { // Draw Location Marker
                            Pen pen;
                            if (unit.Value.IsAlly) pen = grn;
                            else if (unit.Value.IsPMC) pen = red;
                            else if (unit.Value.IsPlayerScav) pen = wht;
                            else if (unit.Value.IsScavBoss) pen = vlt;
                            else if (unit.Value.IsScav) pen = ylw;
                            else pen = ylw; // Default
                            gr.DrawLine(pen, new Point(unitPos.X - strokeLength, unitPos.Y), new Point(unitPos.X + strokeLength, unitPos.Y));
                            gr.DrawLine(pen, new Point(unitPos.X, unitPos.Y - strokeLength), new Point(unitPos.X, unitPos.Y + strokeLength));
                        }
                    }
                    /// ToDo Handle Units Dying, draw a marker on death location
                    /// Handle Loot/Items
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
        /// ToDo
        /// </summary>
        private MapPosition VectorToPositions(UnityEngine.Vector3 vector)
        {
            return new MapPosition();
        }
    }
}

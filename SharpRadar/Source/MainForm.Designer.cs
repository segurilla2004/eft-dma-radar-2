namespace SharpRadar
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mapCanvas = new System.Windows.Forms.PictureBox();
            this.trackBar_Zoom = new System.Windows.Forms.TrackBar();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button_Map = new System.Windows.Forms.Button();
            this.label_Map = new System.Windows.Forms.Label();
            this.label_Pos = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.mapCanvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // mapCanvas
            // 
            this.mapCanvas.ImageLocation = "";
            this.mapCanvas.Location = new System.Drawing.Point(0, 0);
            this.mapCanvas.Name = "mapCanvas";
            this.mapCanvas.Size = new System.Drawing.Size(900, 900);
            this.mapCanvas.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.mapCanvas.TabIndex = 1;
            this.mapCanvas.TabStop = false;
            // 
            // trackBar_Zoom
            // 
            this.trackBar_Zoom.Location = new System.Drawing.Point(169, 19);
            this.trackBar_Zoom.Maximum = 99;
            this.trackBar_Zoom.Name = "trackBar_Zoom";
            this.trackBar_Zoom.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_Zoom.Size = new System.Drawing.Size(45, 349);
            this.trackBar_Zoom.TabIndex = 6;
            this.trackBar_Zoom.Scroll += new System.EventHandler(this.trackBar_Zoom_Scroll);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label_Pos);
            this.groupBox1.Controls.Add(this.label_Map);
            this.groupBox1.Controls.Add(this.button_Map);
            this.groupBox1.Controls.Add(this.trackBar_Zoom);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Right;
            this.groupBox1.Location = new System.Drawing.Point(919, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(226, 925);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Testing Controls";
            // 
            // button_Map
            // 
            this.button_Map.Location = new System.Drawing.Point(38, 29);
            this.button_Map.Name = "button_Map";
            this.button_Map.Size = new System.Drawing.Size(75, 23);
            this.button_Map.TabIndex = 7;
            this.button_Map.Text = "Map";
            this.button_Map.UseVisualStyleBackColor = true;
            this.button_Map.Click += new System.EventHandler(this.button_Map_Click);
            // 
            // label_Map
            // 
            this.label_Map.AutoSize = true;
            this.label_Map.Location = new System.Drawing.Point(35, 55);
            this.label_Map.Name = "label_Map";
            this.label_Map.Size = new System.Drawing.Size(79, 13);
            this.label_Map.TabIndex = 8;
            this.label_Map.Text = "DEFAULTMAP";
            // 
            // label_Pos
            // 
            this.label_Pos.AutoSize = true;
            this.label_Pos.Location = new System.Drawing.Point(35, 100);
            this.label_Pos.Name = "label_Pos";
            this.label_Pos.Size = new System.Drawing.Size(0, 13);
            this.label_Pos.TabIndex = 9;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1145, 925);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.mapCanvas);
            this.Name = "MainForm";
            this.Text = "SharpRadar";
            ((System.ComponentModel.ISupportInitialize)(this.mapCanvas)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox mapCanvas;
        private System.Windows.Forms.TrackBar trackBar_Zoom;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button_Map;
        private System.Windows.Forms.Label label_Pos;
        private System.Windows.Forms.Label label_Map;
    }
}


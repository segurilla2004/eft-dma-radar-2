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
            this.label_Pos = new System.Windows.Forms.Label();
            this.label_Map = new System.Windows.Forms.Label();
            this.button_Map = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.trackBar_AimLength = new System.Windows.Forms.TrackBar();
            this.trackBar_EnemyAim = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.mapCanvas)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AimLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_EnemyAim)).BeginInit();
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
            this.trackBar_Zoom.Location = new System.Drawing.Point(169, 160);
            this.trackBar_Zoom.Maximum = 99;
            this.trackBar_Zoom.Name = "trackBar_Zoom";
            this.trackBar_Zoom.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_Zoom.Size = new System.Drawing.Size(45, 349);
            this.trackBar_Zoom.TabIndex = 6;
            this.trackBar_Zoom.Scroll += new System.EventHandler(this.trackBar_Zoom_Scroll);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.trackBar_EnemyAim);
            this.groupBox1.Controls.Add(this.trackBar_AimLength);
            this.groupBox1.Controls.Add(this.label1);
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
            // label_Pos
            // 
            this.label_Pos.AutoSize = true;
            this.label_Pos.Location = new System.Drawing.Point(35, 100);
            this.label_Pos.Name = "label_Pos";
            this.label_Pos.Size = new System.Drawing.Size(0, 13);
            this.label_Pos.TabIndex = 9;
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(166, 144);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Zoom";
            // 
            // trackBar_AimLength
            // 
            this.trackBar_AimLength.LargeChange = 50;
            this.trackBar_AimLength.Location = new System.Drawing.Point(102, 160);
            this.trackBar_AimLength.Maximum = 1000;
            this.trackBar_AimLength.Minimum = 100;
            this.trackBar_AimLength.Name = "trackBar_AimLength";
            this.trackBar_AimLength.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_AimLength.Size = new System.Drawing.Size(45, 349);
            this.trackBar_AimLength.SmallChange = 5;
            this.trackBar_AimLength.TabIndex = 11;
            this.trackBar_AimLength.Value = 500;
            // 
            // trackBar_EnemyAim
            // 
            this.trackBar_EnemyAim.LargeChange = 50;
            this.trackBar_EnemyAim.Location = new System.Drawing.Point(38, 160);
            this.trackBar_EnemyAim.Maximum = 1000;
            this.trackBar_EnemyAim.Minimum = 100;
            this.trackBar_EnemyAim.Name = "trackBar_EnemyAim";
            this.trackBar_EnemyAim.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_EnemyAim.Size = new System.Drawing.Size(45, 349);
            this.trackBar_EnemyAim.SmallChange = 5;
            this.trackBar_EnemyAim.TabIndex = 12;
            this.trackBar_EnemyAim.Value = 150;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(88, 144);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Player Aimline";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 144);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Enemy Aimline";
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
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AimLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_EnemyAim)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox mapCanvas;
        private System.Windows.Forms.TrackBar trackBar_Zoom;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button_Map;
        private System.Windows.Forms.Label label_Pos;
        private System.Windows.Forms.Label label_Map;
        private System.Windows.Forms.TrackBar trackBar_AimLength;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar trackBar_EnemyAim;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
    }
}


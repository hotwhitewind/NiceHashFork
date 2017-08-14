namespace WixSharp_Setup1
{
    partial class AdditionalOptionsDlg
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
            this.NextButton = new WixSharp.Controls.WixButton();
            this.wixTextBoxIdNumber = new WixSharp.Controls.WixTextBox();
            this.wixControl2 = new WixSharp.Controls.WixControl();
            this.wixLabel1 = new WixSharp.Controls.WixLabel();
            this.BackButton = new WixSharp.Controls.WixButton();
            this.CancelButton = new WixSharp.Controls.WixButton();
            this.wixControl1 = new WixSharp.Controls.WixControl();
            this.wixLabel2 = new WixSharp.Controls.WixLabel();
            this.wixControl3 = new WixSharp.Controls.WixControl();
            this.wixLabel3 = new WixSharp.Controls.WixLabel();
            this.SuspendLayout();
            // 
            // NextButton
            // 
            this.NextButton.BoundProperty = null;
            this.NextButton.Hidden = false;
            this.NextButton.Id = "Next";
            this.NextButton.Location = new System.Drawing.Point(307, 322);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(75, 23);
            this.NextButton.Text = "Next";
            this.NextButton.Tooltip = null;
            this.NextButton.WixAttributes = "Default=yes";
            // 
            // wixTextBoxIdNumber
            // 
            this.wixTextBoxIdNumber.BoundProperty = "IDENTNUMBER";
            this.wixTextBoxIdNumber.Hidden = false;
            this.wixTextBoxIdNumber.Id = null;
            this.wixTextBoxIdNumber.Location = new System.Drawing.Point(11, 95);
            this.wixTextBoxIdNumber.Name = "wixTextBoxIdNumber";
            this.wixTextBoxIdNumber.Size = new System.Drawing.Size(263, 20);
            this.wixTextBoxIdNumber.Tooltip = null;
            this.wixTextBoxIdNumber.WixAttributes = null;
            // 
            // wixControl2
            // 
            this.wixControl2.BoundProperty = null;
            this.wixControl2.ControlType = WixSharp.Controls.ControlType.Bitmap;
            this.wixControl2.EmbeddedXML = null;
            this.wixControl2.Hidden = false;
            this.wixControl2.Id = null;
            this.wixControl2.Location = new System.Drawing.Point(0, 0);
            this.wixControl2.Name = "wixControl2";
            this.wixControl2.Size = new System.Drawing.Size(492, 51);
            this.wixControl2.Text = "!(loc.LicenseAgreementDlgBannerBitmap)";
            this.wixControl2.Tooltip = null;
            this.wixControl2.WixAttributes = "TabSkip=no";
            this.wixControl2.WixText = "!(loc.LicenseAgreementDlgBannerBitmap)";
            // 
            // wixLabel1
            // 
            this.wixLabel1.BoundProperty = null;
            this.wixLabel1.Hidden = false;
            this.wixLabel1.Id = "Title";
            this.wixLabel1.Location = new System.Drawing.Point(12, 9);
            this.wixLabel1.Name = "wixLabel1";
            this.wixLabel1.Size = new System.Drawing.Size(189, 13);
            this.wixLabel1.Text = "{\\WixUI_Font_Title}Product Activation";
            this.wixLabel1.Tooltip = null;
            this.wixLabel1.WixAttributes = "Transparent=yes;NoPrefix=yes";
            // 
            // BackButton
            // 
            this.BackButton.BoundProperty = null;
            this.BackButton.Hidden = false;
            this.BackButton.Id = "Back";
            this.BackButton.Location = new System.Drawing.Point(220, 322);
            this.BackButton.Name = "BackButton";
            this.BackButton.Size = new System.Drawing.Size(75, 23);
            this.BackButton.Text = "Back";
            this.BackButton.Tooltip = null;
            this.BackButton.WixAttributes = null;
            // 
            // CancelButton
            // 
            this.CancelButton.BoundProperty = null;
            this.CancelButton.Hidden = false;
            this.CancelButton.Id = "Cancel";
            this.CancelButton.Location = new System.Drawing.Point(403, 322);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.Text = "Cancel";
            this.CancelButton.Tooltip = null;
            this.CancelButton.WixAttributes = null;
            // 
            // wixControl1
            // 
            this.wixControl1.BoundProperty = null;
            this.wixControl1.ControlType = WixSharp.Controls.ControlType.Billboard;
            this.wixControl1.EmbeddedXML = null;
            this.wixControl1.Hidden = false;
            this.wixControl1.Id = null;
            this.wixControl1.Location = new System.Drawing.Point(1, 306);
            this.wixControl1.Name = "wixControl1";
            this.wixControl1.Size = new System.Drawing.Size(491, 1);
            this.wixControl1.Tooltip = null;
            this.wixControl1.WixAttributes = null;
            this.wixControl1.WixText = "";
            // 
            // wixLabel2
            // 
            this.wixLabel2.BoundProperty = null;
            this.wixLabel2.Hidden = false;
            this.wixLabel2.Id = null;
            this.wixLabel2.Location = new System.Drawing.Point(12, 69);
            this.wixLabel2.Name = "wixLabel2";
            this.wixLabel2.Size = new System.Drawing.Size(270, 13);
            this.wixLabel2.Text = "Input card number or telephone number for registration";
            this.wixLabel2.Tooltip = null;
            this.wixLabel2.WixAttributes = null;
            // 
            // wixControl3
            // 
            this.wixControl3.BoundProperty = "PAY_SYSTEM";
            this.wixControl3.ControlType = WixSharp.Controls.ControlType.ComboBox;
            this.wixControl3.EmbeddedXML = "<ComboBox Property=\"PAY_SYSTEM\">\r\n<ListItem Text = \"VISA/MasterCard\" Value=\"VISA/" +
    "MasterCard\" />\r\n<ListItem Text = \"QIWI\" Value=\"QIWI\"/>\r\n</ComboBox>";
            this.wixControl3.Hidden = false;
            this.wixControl3.Id = null;
            this.wixControl3.Location = new System.Drawing.Point(12, 144);
            this.wixControl3.Name = "wixControl3";
            this.wixControl3.Size = new System.Drawing.Size(120, 20);
            this.wixControl3.Tooltip = null;
            this.wixControl3.WixAttributes = null;
            this.wixControl3.WixText = "";
            // 
            // wixLabel3
            // 
            this.wixLabel3.BoundProperty = null;
            this.wixLabel3.Hidden = false;
            this.wixLabel3.Id = null;
            this.wixLabel3.Location = new System.Drawing.Point(11, 128);
            this.wixLabel3.Name = "wixLabel3";
            this.wixLabel3.Size = new System.Drawing.Size(118, 13);
            this.wixLabel3.Text = "Choice your pay system";
            this.wixLabel3.Tooltip = null;
            this.wixLabel3.WixAttributes = null;
            // 
            // AdditionalOptionsDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(490, 358);
            this.Controls.Add(this.wixLabel3);
            this.Controls.Add(this.wixControl3);
            this.Controls.Add(this.wixLabel2);
            this.Controls.Add(this.wixControl1);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.BackButton);
            this.Controls.Add(this.wixLabel1);
            this.Controls.Add(this.wixControl2);
            this.Controls.Add(this.wixTextBoxIdNumber);
            this.Controls.Add(this.NextButton);
            this.Name = "AdditionalOptionsDlg";
            this.Text = "[ProductName] Setup";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private WixSharp.Controls.WixButton NextButton;
        private WixSharp.Controls.WixTextBox wixTextBoxIdNumber;
        private WixSharp.Controls.WixControl wixControl2;
        private WixSharp.Controls.WixLabel wixLabel1;
        private WixSharp.Controls.WixButton BackButton;
        private WixSharp.Controls.WixButton CancelButton;
        private WixSharp.Controls.WixControl wixControl1;
        private WixSharp.Controls.WixLabel wixLabel2;
        private WixSharp.Controls.WixControl wixControl3;
        private WixSharp.Controls.WixLabel wixLabel3;
    }
}
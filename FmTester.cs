using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace AhDung.WinForm
{
    public partial class FmTester : Form
    {
        MessageBoxButtons msgButtons;
        MessageBoxIcon msgIcon;
        MessageBoxDefaultButton msgDfButton;

        public FmTester()
        {
            InitializeComponent();

            grpMsbButtons.Tag = typeof(MessageBoxButtons);
            grpMsgIcon.Tag = typeof(MessageBoxIcon);
            grpDfButton.Tag = typeof(MessageBoxDefaultButton);

            msgButtons = MessageBoxButtons.OK;
            msgIcon = MessageBoxIcon.None;
            msgDfButton = MessageBoxDefaultButton.Button1;
        }

        private void btnShowMsgEx_Click(object sender, EventArgs e)
        {
            txbResult.AppendText(
            MsgBox.Show(txbMessage.Text
                , txbCaption.Text
                , txbAttachMessage.Text
                , msgButtons
                , msgIcon
                , msgDfButton
                , ckbExpand.Checked
                , txbButtonsText.Lines
                )
                + "\r\n");
        }

        private void btnShowMsgStd_Click(object sender, EventArgs e)
        {
            txbResult.AppendText(
            MessageBox.Show(txbMessage.Text
                , txbCaption.Text
                , msgButtons
                , msgIcon
                , msgDfButton
                )
                + "\r\n");
        }

        private void RadioButtons_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (!rb.Checked) { return; }

            Type t = rb.Parent.Tag as Type;
            object val = Enum.Parse(t, rb.Tag?.ToString() ?? rb.Text);
            if (t == typeof(MessageBoxButtons)) { msgButtons = (MessageBoxButtons)val; }
            else if (t == typeof(MessageBoxIcon)) { msgIcon = (MessageBoxIcon)val; }
            else { msgDfButton = (MessageBoxDefaultButton)val; }
        }

        private void ckbEnableAnimate_CheckedChanged(object sender, EventArgs e)
        {
            MsgBox.EnableAnimate = ckbEnableAnimate.Checked;
        }

        private void ckbEnableSound_CheckedChanged(object sender, EventArgs e)
        {
            MsgBox.EnableSound = ckbEnableSound.Checked;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.cnblogs.com/ahdung/p/4631233.html");
        }

        private void btnShowInfo_Click(object sender, EventArgs e)
        {
            MsgBox.ShowInfo(txbMessage.Text, txbAttachMessage.Text, txbCaption.Text, ckbExpand.Checked, GetFirstLine(txbButtonsText));
        }

        private void btnShowWarning_Click(object sender, EventArgs e)
        {
            MsgBox.ShowWarning(txbMessage.Text, txbAttachMessage.Text, txbCaption.Text, ckbExpand.Checked, GetFirstLine(txbButtonsText));
        }

        private void btnShowError_Click(object sender, EventArgs e)
        {
            MsgBox.ShowError(txbMessage.Text, txbAttachMessage.Text, txbCaption.Text, ckbExpand.Checked, GetFirstLine(txbButtonsText));
        }

        private void btnShowQuestion_Click(object sender, EventArgs e)
        {
            var result = MsgBox.ShowQuestion(txbMessage.Text, txbAttachMessage.Text, defaultButton: msgDfButton, expand: ckbExpand.Checked, buttonsText: txbButtonsText.Lines);
            txbResult.AppendText($"{result}\r\n");
        }

        static string GetFirstLine(TextBox txb) => txb.Text.Substring(0, txb.Text.IndexOf('\r') + 1);
    }
}

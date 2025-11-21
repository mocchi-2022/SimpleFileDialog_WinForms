using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleFileDialog {
	static class Program {
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// basic sample
			using (var sfd = new SimpleFileDialog(SimpleFileDialog.FileDialogMode.SaveFile)){
				sfd.InitialDirectory = @"c:\Users";
				sfd.Filter = "text files (*.txt)|*.txt|image files|*.png;*.gif;*.jpg;*.jpeg|All Files|*.*";
				sfd.DefaultExt = ".txt";
				sfd.FileName = "HelloWorld";
				sfd.Title = "Hello";
				if (sfd.ShowDialog() == DialogResult.OK) {
					MessageBox.Show(sfd.FileName + " saved.");
				} else {
					MessageBox.Show(sfd.FileName + " not saved.");
				}
			}

			// custom control sample
			using (var sfd = new SimpleFileDialog(SimpleFileDialog.FileDialogMode.SelectFolder)) {
				var layout = new TableLayoutPanel() { ColumnCount = 3, RowCount = 2, Padding = new Padding(1), Margin = new Padding(1) };
				TextBox txtPrefix;
				RadioButton rbText, rbBinary;

				layout.SuspendLayout();
				layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100.0f));
				layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100.0f));
				layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
				layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10.0f));
				layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10.0f));
				layout.Height = 60;
				layout.Controls.Add(
					new Label() { Text = "prefix", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleCenter, AutoSize = false },
					0, 0);
				layout.Controls.Add(
					txtPrefix = new TextBox() { Dock = DockStyle.Fill },
					1, 0);
				layout.SetColumnSpan(txtPrefix, 2);
				layout.Controls.Add(
					rbText = new RadioButton() { Dock = DockStyle.Left, Text = "text", AutoSize = true, Checked = true },
					0, 1);
				layout.Controls.Add(
					rbBinary = new RadioButton() { Dock = DockStyle.Left, Text = "binary", AutoSize = true },
					1, 1);

				layout.ResumeLayout();


				sfd.CustomControl = layout;

				sfd.InitialDirectory = @"c:\Users";
				sfd.Filter = "text files (*.txt)|*.txt|image files|*.png;*.gif;*.jpg;*.jpeg|All Files|*.*";
				sfd.DefaultExt = ".txt";
				sfd.Multiselect = true;
				sfd.FileName = "HelloWorld";
				sfd.CheckFileExists = true;
				//				sfd.Title = "Hello";
				if (sfd.ShowDialog() == DialogResult.OK) {
					MessageBox.Show(sfd.FileName + " opened.\r\n"
						+ "|" + string.Join("|", sfd.FileNames.ToArray()) + "| opened.\r\n"
						+ "prefix:" + txtPrefix.Text + ", " + (rbText.Checked ? "text" : "binary"));
				} else {
					MessageBox.Show(sfd.FileName + " not opened.");
					MessageBox.Show("|" + string.Join("|", sfd.FileNames.ToArray()) + "| not opened.");
				}
			}
		}
	}
}

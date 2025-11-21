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

			using (var sfd = new SimpleFileDialog(SimpleFileDialog.FileDialogMode.SaveFile)){
				sfd.InitialDirectory = @"c:\Users";
				sfd.Filter = "text files (*.txt)|*.txt|image files|*.png;*.gif;*.jpg;*.jpeg|All Files|*.*";
				sfd.DefaultExt = ".txt";
//				sfd.Title = "Hello";
				if (sfd.ShowDialog() == DialogResult.OK) {
					MessageBox.Show(sfd.FileName + " saved.");
				} else {
					MessageBox.Show(sfd.FileName + " not saved.");
				}
			}
		}
	}
}

/// Simple File Dialog version 0.2
/// Copylight mocchi 2021
/// Distributed under the Boost Software License, Version 1.0.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

// references
// https://www.codeproject.com/Articles/13097/An-quot-Explorer-Style-quot-TreeView-Control
// https://www.ipentec.com/document/csharp-shell-namespace-create-explorer-tree-view-control-and-linked-list-view
// http://acha-ya.cocolog-nifty.com/blog/2010/09/post-241a.html
// https://nasu38yen.wordpress.com/2010/05/28/%e6%8b%a1%e5%bc%b5%e5%ad%90%e3%81%8b%e3%82%89%e5%b0%8f%e3%81%95%e3%81%aa%e3%82%a2%e3%82%a4%e3%82%b3%e3%83%b3%e3%82%92get%e3%81%99%e3%82%8b%e3%81%ab%e3%81%af%e3%80%81shgetfileinfo%e3%82%92usefileattrib/
// https://dobon.net/vb/bbs/log3-51/30394.html
// https://www.curict.com/item/0a/0a33f42.html

// Todo: select folder
namespace SimpleFileDialog {
	public partial class SimpleFileDialog : Form {

		// properties
		public string InitialDirectory {
			get;
			set;
		}
		public string FileName {
			get;
			set;
		}
		public string Filter {
			get;
			set;
		}
		public string Title {
			get;
			set;
		}
		private string _defaultExt = "";
		public string DefaultExt {
			get { return _defaultExt; }
			set {
				if (value.Length > 0 && value[0] == '.') {
					_defaultExt = value.Substring(1);
				} else {
					_defaultExt = value;
				}
			}
		}

		private FileDialogMode _fileDialogMode;
		public enum FileDialogMode {
			OpenFile, SaveFile, SelectFolder
		}

		private string _currentDirectory = "";
		public string CurrentDirectory {
			get{
				return _currentDirectory;
			}
			set{
				if (!Directory.Exists(value)) {
					throw new DirectoryNotFoundException();
				}
				if (_currentDirectory == value) return;

				try {
					var dirs = Directory.EnumerateDirectories(value);
				} catch {
					MessageBox.Show("access denied", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
				_currentDirectory = value;

				// update undo&redo buffer
				if (_undoRedoBuffer != null) {
					if (_undoIndex < _undoRedoBuffer.Count - 1) _undoRedoBuffer.RemoveRange(_undoIndex + 1, _undoRedoBuffer.Count - _undoIndex - 1);
					_undoRedoBuffer.Add(_currentDirectory);
					++_undoIndex;
					if (_undoIndex > 0) buttonUndo.Enabled = true;
					buttonRedo.Enabled = false;
				}

				RedrawListView();
			}
		}

		private string _customFilterString;
		private int _undoIndex;
		private List<string> _undoRedoBuffer;

		public SimpleFileDialog(FileDialogMode fileDialogMode = FileDialogMode.OpenFile) {
			Filter = "";
			_fileDialogMode = fileDialogMode;
			_undoRedoBuffer = null;
			InitializeComponent();

			switch (fileDialogMode) {
				case FileDialogMode.OpenFile:
					buttonOK.Text = "Open";
					break;
				case FileDialogMode.SaveFile:
					buttonOK.Text = "Save";
					break;
				case FileDialogMode.SelectFolder:
					buttonOK.Text = "Select";
					textBoxFileName.ReadOnly = true;
					textBoxFileName.Text = "Select Folder.";
					break;
			}

			// dialog icons
			var leftArrowBmp = CreateBitmapFromBase64(@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAABnRSTlMAwwDDAMNRly6JAAAAbUlEQVR42mM8fPgwAymAkVIN/TP4gOTh/Q3rVhYR1kBQNYoGYlQjNBCpGqoBopogKMz4BNIgKhVo69hAjAaI/aRrADopKLwPogcohF8PVAOQBdcDcSjhUCJeD0rEEaMHPWkA9cCdS5QGgoBkDQBaUFah4+0qJgAAAABJRU5ErkJggg==");
			buttonUndo.Image = leftArrowBmp;

			var rightArrowBmp = CreateBitmapFromBase64(@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAABnRSTlMAwwDDAMNRly6JAAAAaklEQVR42mM8fPgwAymAkWoa+mfwAcl1S/XRFOCzAaseAk7C1EPYD2h6QBogQgQBRA8JGg7vb1i3sogsDUBOUHgfLnW2jg1w1cSGElw1UfGArJqBYEyjqcanwdbWVlQqEE01YT9gApI1AAASUl+h+XaRoQAAAABJRU5ErkJggg==");
			buttonRedo.Image = rightArrowBmp;

			var upArrowBmp = CreateBitmapFromBase64(@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAABnRSTlMAwwDDAMNRly6JAAAAbklEQVR42mM8fPgwAymAEVND/ww+COPw/oZ1K4sIaICrxqUHRQOaaqx6EBogqoHSto4NcKUQNrIeqAa4aqAEsh+AJJoehAa4EJqng8L7gHrWLdWHqIRqAIrCLcUMJWRZioN1pGqAhCOEQawGPAAA17yHoW6n2a8AAAAASUVORK5CYII=");
			buttonUp.Image = upArrowBmp;

			var reloadBmp = CreateBitmapFromBase64(@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAABnRSTlMAwwDDAMNRly6JAAAAlklEQVR42mM8fPgwAzYQ/64TSC4UKkcTZ8SjYbLpxdzT+mh6UDRATIUDoAYgiaYHoQFiJFbbvKMeH24+jKIBj2rCGoDSEMbWZbJoqqEa0FRDpG1rbYEa0FSja0CWBmoAkmiq8WnAGnTA4CJKA7IsZRowowkzPKDBCgkTuB60yEa2HBHTyHoIRxwePVgiDlkaEvzIAC0YAOE3raHJ0QpxAAAAAElFTkSuQmCC");
			buttonRedraw.Image = reloadBmp;

			var listBmp = CreateBitmapFromBase64(@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAPElEQVR42mOsr69nIAUwAjU0NDQQqRqoEqrBwfMskH9guzHJGhgZGbEq/f//P4qGUSeNFCeRpoFI1RAAAKkAZeGr5RELAAAAAElFTkSuQmCC");
			radioButtonList.Image = listBmp;

			var detailBmp = CreateBitmapFromBase64(@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAUUlEQVR42mOsr69nIAUwAjW4uLhgSuzZswcuDmcDGWRpOHDSF8g5sN2YoHsaGhpQNDAyMsLl/v//D+fC2UDFIA1AfUT6GN2GUScNLScRqRoCAK43deFZIhv/AAAAAElFTkSuQmCC");
			radioButtonDetail.Image = detailBmp;
		}

		private Dictionary<string, int> _extensionToImageListIndex;

		private void SimpleFileDialog_Load(object sender, EventArgs e) {

			if (!string.IsNullOrEmpty(Title)) {
				Text = Title;
			} else if (_fileDialogMode == FileDialogMode.OpenFile){
				Text = "Open";
			} else if (_fileDialogMode == FileDialogMode.SaveFile) {
				Text = "Save as";
			} else if (_fileDialogMode == FileDialogMode.SelectFolder) {
				Text = "Select folder";
			}


			// folder / file icon
			var imageList = new ImageList();
			var iconsInfo = new[]{
				// === special icons ===
				// folder
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAABnRSTlMAAAAAAABupgeRAAAARklEQVR42mP8//8/AymAkRwNl1cxo4nqhv0lTYO2WRqmUiaF6VAN/x5kEuOYPbPfu7WuGNUweDQAObuqI4jRg9BAPCBZAwB33G7hdlLspwAAAABJRU5ErkJggg==",
				},
				// any file
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAATklEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXQJQGIMPW1hbieMIaIIzGxkaiNCCczsg4qmGwaQByGIgACA3EqIYDANMbhuGW3OtEAAAAAElFTkSuQmCC",
				},

				// === icons for each extensions from here ===
				// 3d shape
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAs0lEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXgKEhuwlFg+g/oAYgw9bWFuJ4VA3ZTZ+qYpE19M1eCGE0NjZiaMBQDQHTZczL/79iZGTEogHIR9PD17a4c9oUnBqAqoEqkDXsErh2vm0fPg0QdS8uHgGSD45uVbD2Xu9dTEADULWEvg1cz0DagBQPaKpB6rBrYGDoZBRDDiWIanQNQA4x6RShgdiUDQYA2nzn4SxnIr4AAAAASUVORK5CYII=",
					".stl", ".ply", ".wrl", ".fbx", ".3mf", ".glb", ".3dm", ".stp", ".step", ".igs", ".iges", ".3ds", ".blend"
				},
				// archive
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAcklEQVR42mP8//8/AymAkRwNl1cxo4nqhv0lTYO2WRqmUiaF6VAN/x5kEuOYPbPfu7WuIEtDQ0ODi4vLnj178JC2trY7q8KhGo4cOUJQNcQSoGKinMSsOAOklJGRlhpIcxLFwQrk7KqOIEYPQgPxgGQNABK3kuF10wpFAAAAAElFTkSuQmCC",
					".zip", ".tar", ".gz", ".tgz", ".bz2", ".tbz", ".lzh", ".7z", ".rar", ".msi"
				},
				// data
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAY0lEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXQJQGIMPW1hbieIQGRkZGTA319fUQRmNjI1EaIACoBiiLRQOKBCoDu4aRZwOBJESRBjyOQXMYVAMxquEAAAQVtuHe59v8AAAAAElFTkSuQmCC",
					".csv", ".xml", ".json", ".db", ".db3", ".sqlite", ".sqlite3", ".sqlitedb", ".mdb", ".dat"
				},
				// diskimage
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAu0lEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXQJQGIMPW1hbieMIaIIzGxkbsGtB0IlzCyIhFA5A8cNIXyDgYJbPVxRTI8JZ6DFWHqQEIVE6pzvmvgazBi+cxIx8DUA1ODUCh2O0xEGcs9lwC4eLU0FDcuO2LLMgle07bL3sCZKQw3sCpAeIHs/R5QNVANkTDwR0mOP0ADw3kUIInHJwacCYhijSgOQMXQGggRjUcAACHcMThthqWBgAAAABJRU5ErkJggg==",
					".iso", ".img", ".vmdk", ".vhd"
				},
				// electrical document
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAdklEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXgNDg4Hn2wHZjIImswcF8M1ADkGFrawtxPGENEEZjYyO6BgJ+ZWRE1wAUwqUaqAaLBvJtAIqg+QToNwI2EKVhkNtAIJTwxABabEA1EKMaDgDm5qnhbYXBrwAAAABJRU5ErkJggg==",
					".pdf", ".xps", ".ps", ".dvi"
				},
				// executable
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAASklEQVR42mP8//8/AymAkRwNDQ0NxChtbGwEKoZqkIrKxqN0Wf4jIHlwhwkFGhITEwk6SUFBYVQDjTUAo52gBiCAaiBGKRyQrAEA0I194QBfsWoAAAAASUVORK5CYII=",
					".exe", ".bat", ".js", ".vbs", ".sh"
				},
				// image
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAjElEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXQJQGIMPW1hbieIQGRkZGTA319fUQRmNjIxYNM2++wtSTri4GVAOUxa6h80YSRF25xjzCGpQ2+qAZD9RGmgYguOe/BaeGrc9kMTV4Sz0eQA24kgZ2DfiTE4oGPGaj2QPVQIxqOAAAiNe24ei+CfsAAAAASUVORK5CYII=",
					".bmp", ".png", ".jpg", ".jfif", ".jpeg", ".tif", ".tiff", ".gif", " .dicom", ".xbm", ".xpm", ".ppm", ".pgm", ".pbm", ".ico", ".svg", ".vml", ".wmf", ".emf", ".eps", ".psd", ".ai", ".hdr", ".exr", ".rgbe"
				},
				// markup
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAdElEQVR42q2R0Q3AIAhEe4P5516FvdyNkmDEWFqtKV+n3MuBQkSOLwUFiOjd1Bsq8MTofc65lNIMDgAY3NZSQHVKyYafJ5hg5hEIE3x0YClhDoAhpwQ6BHrHwASA97gu044q/kjY2WHnlZaA+5eF5cCKu9UF2Kat4Xr11NsAAAAASUVORK5CYII=",
					".htm", ".html", ".shtml", ".md", ".rtf"
				},
				// multimedia
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAbklEQVR42rWS0Q3AIAhEy2D8OZjuxW70Eiy1tlZsUr4OuBfQQKq6rQQByDm/m1pDBUYM6iklEXFDCIBgZlt+DpgopQwBIqq9Q1yKDjy378IATEQC3TnmQGf9AYiutPzoL986ubkWQBK50xOIuD12HVDU4SoPg2UAAAAASUVORK5CYII=",
					".wav", ".mid", ".midi", ".mp3", ".flac", ".mpg", "mpeg", ".mp4", ".wmv", ".wma", ".3gp"
				},
				// office
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAArUlEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXgNDAyMh49Sq681atAmkAMmxtbSGOx6dBwl11UnI0hN3Y2IhFAwMDSOjqVajqFztvQ1RraTEAZXHaoK3N8FYGqhrIBurHpwFoMFBU+MltoDqIanw2vJVRgTsDqBruPAJOwgQ4NeCKOKAaLBoIpAhMDQ6eZ3GpPrDdmDIb8LgezSdQDcSohgMAeUqz4S+k07IAAAAASUVORK5CYII=",
					".xls", ".xlsx", ".xlsm", ".ppt", ".pptx", ".pptm", ".doc", ".docx", ".docm", ".ods", ".odp", ".odt",
				},
				// plane text
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAW0lEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXQJQGIMPW1hbieIQGRkZGNNXIrm1sbETXQMCvjIyEbUC2CouGURuobAMes9HsgWogRjUcAACu0JXhKcCftQAAAABJRU5ErkJggg==",
					".txt", ".text"
				},
				// program source
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAf0lEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXQJQGIMPW1hbieBQNDp5ngeSB7cYQ1UCug/lmCLuxsRG7BrhquB6ICCMjI04NEHsgVhGlgVgb4HLINsD1UMMG2vsBazzAudg14EtzyBqAHGLSKUIDMarhAADlYcPhd4fpHAAAAABJRU5ErkJggg==",
					".c", ".cc", ".cpp", ".cxx", ".cs", ".go", ".h", ".hpp", ".hxx", ".vb", ".java", ".tex", ".tcl", ".pl", ".py", ".rs", ".rb", ".asp", ".aspx", ".php", ".lua"
				},
				// runtime module
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAvUlEQVR42mP8//8/AymAEaihoaGBGKWNjY1AxVANUlHZBDWkq4tRoCExMZGgBgUFBZAGe48zcKGFM0SAZHzGGzQRdA1RE+WW5T9ClgPqwRRE14BmA1wQLo6iAciHS0NUQ0SQzULRsDw9LHLmKggJCZCZN1/B9UPchm4DUDVcBUQbPg0Q1c3NzUDS1tYWSAJtwOckiA0HDhyAGAlxD9xXB3eYQBgIDXBHQxgQEqjhwHZj9MSHHHGYAIsGUpM3AC7bsOFlNXRFAAAAAElFTkSuQmCC",
					".dll", ".so", ".class", ".sys"
				},
				// shortcut
				new[]{
					@"iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAIAAACQkWg2AAAAiUlEQVR42mP8//8/AymAEaihoaEBvyJkBVANuPQAxV1cXPbs2QNXgNDw4elGTA0TZp8HagAybG1tIY4nrAHCaGxsJEoDBAhI+zMyMpKrAX/4vH+yAYsGXFEBVIRTAyOGTUBZfBpgRpKiAc0SAhrgqiGWAblEaYB7Hq4BybV4NcAVoTqSiHjADHEAVeaf4RiLmTwAAAAASUVORK5CYII=",
					".lnk"
				},
			};
			_extensionToImageListIndex = new Dictionary<string, int>();
			foreach (var iconInfo in iconsInfo) {
				var curIndex = imageList.Images.Count;
				for (var i = 1; i < iconInfo.Length; ++i) {
					_extensionToImageListIndex.Add(iconInfo[i], curIndex);
				}
				imageList.Images.Add(CreateBitmapFromBase64(iconInfo[0]));
			}

			listViewFileList.Clear();
			listViewFileList.SmallImageList = imageList;
			listViewFileList.Columns.Add("Name", 400);
			listViewFileList.Columns.Add("Date modified", 150);
			listViewFileList.Columns.Add("File size", 100);
			listViewFileList.Columns[2].TextAlign = HorizontalAlignment.Right;
			listViewFileList.View = View.Details;

			comboBoxFilter.DataSource = null;
			if (Filter != null) {
				var filters = Enumerable.Range(0, 0).Select(v => new { Display = "", Value = "" }).ToList();
				var filtersStr = Filter.Split('|');
				for (var i = 0; i < filtersStr.Length - 1; i += 2) {
					filters.Add(new { Display = filtersStr[i], Value = filtersStr[i + 1] });
				}
				if (filters.Count > 0) {
					comboBoxFilter.DataSource = filters.ToArray();
					comboBoxFilter.DisplayMember = "Display";
					comboBoxFilter.ValueMember = "Value";
				}
			}

			_customFilterString = "";

			// initialize undo&redo buffer
			_undoIndex = -1;
			_undoRedoBuffer = new List<string>();

			CurrentDirectory = InitialDirectory;
		}

		private void RedrawListView() {
			listViewFileList.Items.Clear();
			if (!Visible || !Directory.Exists(_currentDirectory)) {
				return;
			}

			var cursor = Cursor.Current;
			try {

				Cursor.Current = Cursors.WaitCursor;

				var items = new List<ListViewItem>();
				// display folders and files.

				var dirs = Directory.GetDirectories(_currentDirectory);
				string[] files;
				if (!string.IsNullOrEmpty(_customFilterString)) {
					var filter = _customFilterString;
					if (filter.Length == 0 || filter[filter.Length - 1] != '.') filter += '*';
					files = Directory.GetFiles(_currentDirectory, filter);
				} else {
					var filters = comboBoxFilter.Items.Count > 0 ? ((string)comboBoxFilter.SelectedValue).Split(';') : null;
					files = filters.SelectMany(filter => Directory.GetFiles(_currentDirectory, filter)).ToArray();
				}
				switch (listViewFileList.View) {
					case View.List: {
							// directories
							foreach (var dir in dirs) {
								var lvi = new ListViewItem(Path.GetFileName(dir));
								lvi.ImageIndex = 0;
								items.Add(lvi);
							}

							// files
							foreach (var file in files) {
								var lvi = new ListViewItem(Path.GetFileName(file));

								var ext = Path.GetExtension(file).ToLower();
								int idx;
								if (!_extensionToImageListIndex.TryGetValue(ext, out idx)) {
									idx = 1;
								}
								lvi.ImageIndex = idx;
								items.Add(lvi);
							}
						}

						break;
					case View.Details: {
							// directories
							foreach (var dir in dirs) {
								var lvi = new ListViewItem(new string[] { Path.GetFileName(dir), "", "" });
								lvi.ImageIndex = 0;
								items.Add(lvi);
							}

							// files
							var unitname = new string[] { " KB", " MB", " GB", " TB" };
							foreach (var file in files) {
								var fi = new FileInfo(file);

								var fileSize = fi.Length;
								string fileSizeStr = "> 10 TB";

								long denom = 1024L;
								for (var i = 0; i < 4; ++i) {
									if (fileSize < denom * 10240L) {
										long reminder;
										long quot = Math.DivRem(fileSize, denom, out reminder);
										fileSizeStr = (quot + (reminder > 0L ? 1L : 0L)).ToString() + unitname[i];
										break;
									}
									denom *= 1024L;
								}

								var ext = Path.GetExtension(file).ToLower();
								int idx;
								if (!_extensionToImageListIndex.TryGetValue(ext, out idx)) {
									idx = 1;
								}

								var lvi = new ListViewItem(new string[] { Path.GetFileName(file), fi.LastWriteTime.ToString(), fileSizeStr });
								lvi.ImageIndex = idx;
								items.Add(lvi);
							}
						}
						break;
				}
				textBoxTargetFolder.Text = CurrentDirectory;

				listViewFileList.SuspendLayout();
				foreach (var itm in items) {
					listViewFileList.Items.Add(itm);
				}
				listViewFileList.ResumeLayout();
			} finally {
				Cursor.Current = cursor;
			}
		}

		private bool CheckValid(string path, bool withWildcard) {
			var invalidPathChars = withWildcard ? 
				Path.GetInvalidPathChars().Concat(new[] { '*', '?' }).ToArray() : Path.GetInvalidPathChars();
			if (path.IndexOfAny(invalidPathChars) >= 0
				|| Regex.IsMatch(path, @"(^|\\|/)(CON|PRN|AUX|NUL|CLOCK\$|COM[0-9]|LPT[0-9])(\.|\\|/|$)", RegexOptions.IgnoreCase)) {
				return false;
			}
			return true;
		}

		private bool Confirm() {
			var target = textBoxFileName.Text;
			if (_fileDialogMode != FileDialogMode.SelectFolder && string.IsNullOrEmpty(target)) return false;

			if (!CheckValid(target, true)) {
				MessageBox.Show(this, "invalid name", "error", MessageBoxButtons.OK);
				return false;
			}

			var fullPath = Path.Combine(_currentDirectory, target);

			bool exists = File.Exists(fullPath);

			if (_fileDialogMode == FileDialogMode.OpenFile && !exists) {
				MessageBox.Show(this, target + " not found.", "file not found", MessageBoxButtons.OK);
				return false;
			} else if (_fileDialogMode == FileDialogMode.SaveFile && exists) {
				var rc = MessageBox.Show(this, target + " exists. override?", "confirm to override", MessageBoxButtons.YesNo);
				if (rc == System.Windows.Forms.DialogResult.No) return false;
			}
			return true;
		}

		private Bitmap CreateBitmapFromBase64(string base64) {
			using (var ms = new MemoryStream(System.Convert.FromBase64String(base64), false)) {
				ms.Position = 0;
				return new Bitmap(ms);
			}
		}

		// callbacks
		private void radioButtonList_CheckedChanged(object sender, EventArgs e) {
			listViewFileList.View = View.List;
		}

		private void radioButtonDetails_CheckedChanged(object sender, EventArgs e) {
			listViewFileList.View = View.Details;
		}

		private void buttonRedraw_Click(object sender, EventArgs e) {
			RedrawListView();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.Cancel;
			Close();
		}

		private void SimpleFileDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (DialogResult == System.Windows.Forms.DialogResult.OK){
				if (!Confirm()) {
					e.Cancel = true;
					FileName = "";
				} else {
					var fileName = Path.Combine(CurrentDirectory, textBoxFileName.Text);
					if (!Path.HasExtension(fileName) && !string.IsNullOrEmpty(DefaultExt)){
						fileName += "." + DefaultExt;
					}
					FileName = fileName;
				}
			}
		}

		private void buttonUp_Click(object sender, EventArgs e) {
			var parentDir = Path.GetDirectoryName(CurrentDirectory);
			if (!Directory.Exists(parentDir)) return;

			CurrentDirectory = parentDir;
		}

		private void textBoxTargetFolder_KeyPress(object sender, KeyPressEventArgs e) {
			if (e.KeyChar == (char)Keys.Enter) {
				var newDir = textBoxTargetFolder.Text;
				if (Directory.Exists(newDir)) {
					CurrentDirectory = newDir;
				} else {
					textBoxTargetFolder.Text = CurrentDirectory;
				}
				e.Handled = true;
			}
		}

		private void textBoxFileName_KeyPress(object sender, KeyPressEventArgs e) {
			if (e.KeyChar == (char)Keys.Enter) {
				var fileName = textBoxFileName.Text;
				var newDir = "";
				if (Directory.Exists(fileName)) {
					CurrentDirectory = fileName;
					textBoxFileName.Text = "";
				} else if (CheckValid(fileName, false)) {
					if (fileName.IndexOfAny(new char[] {'?', '*'}) >= 0){
						textBoxFileName.Text = fileName;
						textBoxFileName.SelectAll();
						_customFilterString = fileName;
						RedrawListView();
					} else if (Directory.Exists(newDir = Path.Combine(CurrentDirectory, fileName))) {
						CurrentDirectory = newDir;
						textBoxFileName.Text = "";
					} else {
						DialogResult = System.Windows.Forms.DialogResult.OK;
						Close();
					}
				}
				e.Handled = true;
			}
		}

		private void listViewFileList_Click(object sender, EventArgs e) {
			if (listViewFileList.SelectedIndices.Count == 0) return;
			var itm = listViewFileList.SelectedItems[0];
			var fullPath = Path.Combine(CurrentDirectory, itm.Text);

			if (Directory.Exists(fullPath)) {
				return;
			} else {
				textBoxFileName.Text = itm.Text;
			}
		}

		private void listViewFileList_DoubleClick(object sender, EventArgs e) {
			if (listViewFileList.SelectedIndices.Count == 0) return;
			var itm = listViewFileList.SelectedItems[0];
			var fullPath = Path.Combine(CurrentDirectory, itm.Text);

			if (Directory.Exists(fullPath)) {
				if (string.IsNullOrEmpty(_customFilterString)) textBoxFileName.Text = "";
				CurrentDirectory = fullPath;
			} else {
				DialogResult = System.Windows.Forms.DialogResult.OK;
//				FileName = itm.Text;
				Close();
			}
		}

		private void comboBoxFilter_SelectedValueChanged(object sender, EventArgs e) {
			_customFilterString = "";
			RedrawListView();
		}

		private void buttonUndo_Click(object sender, EventArgs e) {
			if (_undoIndex <= 0 && _undoIndex >= _undoRedoBuffer.Count - 1) return;
			--_undoIndex;
			if (_undoIndex < _undoRedoBuffer.Count - 1) buttonRedo.Enabled = true;
			if (_undoIndex == 0) buttonUndo.Enabled = false;
			_currentDirectory = _undoRedoBuffer[_undoIndex];
			RedrawListView();
		}

		private void buttonRedo_Click(object sender, EventArgs e) {
			if (_undoIndex <= 0 && _undoIndex >= _undoRedoBuffer.Count - 1) return;
			++_undoIndex;
			if (_undoIndex == _undoRedoBuffer.Count - 1) buttonRedo.Enabled = false;
			buttonUndo.Enabled = true;
			_currentDirectory = _undoRedoBuffer[_undoIndex];
			RedrawListView();
		}

	}
}

using FileHost.Infra;
using FileHost.Models;
using System.IO;

namespace FileHost.ViewModels
{
	public class FilePreviewVM : ItemPreviewVM
    {
		private int _size;
		private string _extension;

		public int Size
		{
			get => _size;
			set
			{
				if (_size == value) return;
				_size = value;
				OnPropertyChanged("DisplaySize");
			}
		}

		public string Extension
		{
			get => _extension;
			set
			{
				if (_extension == value) return;
				_extension = value;
				OnPropertyChanged();
			}
		}

		public string DisplaySize => $"Size: {new FileSizeFormatter().GetReadableFileSize(Size)}";

        public string DisplayName => Path.GetFileNameWithoutExtension(Item.Name);

        public FilePreviewVM(FileItem item) 
			: base(item)
        {
			Size = item.Size;
			Extension = Path.GetExtension(item.Name)?.Substring(1).ToUpper();
		}
    }
}

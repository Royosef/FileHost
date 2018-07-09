using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FileHost.Infra;
using FileHost.Models;

namespace FileHost.ViewModels
{
	public class ItemPreviewVM : ViewModelBase
    {
		private string _name;
		private string _id;
		private string _revision;
        private bool _isSelected;

		private string FullName { get; set; }
		public HttpClient Client { get; } = new HttpClient {BaseAddress = new Uri("http://127.0.0.1:5984/filehost/")};
		public DelegateCommand DeleteCommand { get; set; }
		public DelegateCommand ToggleSelectCommand { get; set; }

		public string Name
		{
			get => _name;
			set
			{
				if (_name == value) return;
				_name = value;
				OnPropertyChanged();
			}
		}

		public string Id
		{
			get => _id;
			set
			{
				if (_id == value) return;
				_id = value;
				OnPropertyChanged();
			}
		}

		public string Revision
		{
			get => _revision;
			set
			{
				if (_revision == value) return;
				_revision = value;
				OnPropertyChanged();
			}
		}

        public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected == value) return;
			    _isSelected = value;
				OnPropertyChanged();
			}
		}

		public ItemPreviewVM(Item item)
        {
            DeleteCommand = new DelegateCommand(DeleteDocument);
            ToggleSelectCommand = new DelegateCommand(ToggleSelect);

			Id = item.Id;
			Revision = item.Revision;
			FullName = item.Name;
			Name = Path.GetFileNameWithoutExtension(FullName);
		}

        private void ToggleSelect()
        {
            IsSelected = !IsSelected;
        }

        private async void DeleteDocument()
        {
			var f= await Client.DeleteAsync($"{Id}/{FullName}?rev={Revision}");
        }
    }
}

using FileHost.Infra;
using FileHost.Models;

namespace FileHost.ViewModels
{
	public class ItemPreviewVM : ViewModelBase
    {
		private Item _item;
        private bool _isSelected;

		private string FullName { get; }

		public DelegateCommand ToggleSelectCommand { get; }

		public Item Item
		{
			get => _item;
			set
			{
				if (_item == value) return;
			    _item = value;
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
            ToggleSelectCommand = new DelegateCommand(ToggleSelect);

            Item = item;
			FullName = item.Name;
		}

        private void ToggleSelect()
        {
            IsSelected = !IsSelected;
        }
    }
}

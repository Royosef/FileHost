using FileHost.Models;

namespace FileHost.ViewModels
{
	public class FolderPreviewVM : ItemPreviewVM
    {
		private int _itemsAmount;

		public int ItemsAmount
		{
			get => _itemsAmount;
			set
			{
				if (_itemsAmount == value) return;
				_itemsAmount = value;
				OnPropertyChanged("DisplayItemsAmount");
			}
		}

		public string DisplayItemsAmount => $"Items: {ItemsAmount}";

        public FolderPreviewVM(FolderItem item) 
			: base(item)
        {
			ItemsAmount = item.ItemsAmount;
        }
    }
}

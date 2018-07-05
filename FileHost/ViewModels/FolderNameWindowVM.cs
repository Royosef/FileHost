using System;
using System.Windows;
using FileHost.Infra;

namespace FileHost.ViewModels
{ 
    public class FolderNameWindowVM : ViewModelBase
    {
        private bool? _dialogResult;
        private string _folderName;

        public bool? DialogResult
        {
            get => _dialogResult;
            set
            {
                if(_dialogResult == value) return;
                _dialogResult = value;
                OnPropertyChanged();
            }
        }

        public string FolderName
        {
            get => _folderName;
            set
            {
                if (_folderName == value) return;
                _folderName = value;
                OnPropertyChanged();
            }
        }

        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ConfirmCommand { get; }

        public FolderNameWindowVM()
        {
            CancelCommand = new DelegateCommand(Cancel);
            ConfirmCommand = new DelegateCommand(Confirm);
        }

        private void Cancel()
        {
            DialogResult = false;
        }

        private void Confirm()
        {
            if (string.IsNullOrWhiteSpace(FolderName))
            {
                MessageBox.Show("Error: Folder name cannot be empty.");
                return;
            }

            DialogResult = true;
        }
    }
}

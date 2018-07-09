using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using FileHost.Annotations;
using FileHost.Infra;
using FileHost.Models;
using FileHost.Views;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FileHost.ViewModels
{
    public class MainWindowVM : ViewModelBase
    {
        private bool _isEmpty;
        private bool _isMainFolder;
        private string _folderName;
        private FolderItem _currentFolder;

        public FolderItem CurrentFolder
        {
            get => _currentFolder;
            set
            {
                _currentFolder = value;
                FolderName = _currentFolder?.Name;
                IsMainFolder = value == null;
            }
        }

        public bool IsMainFolder
        {
            get => _isMainFolder;
            set
            {
                if (_isMainFolder == value) return;
                _isMainFolder = value;
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

        public HttpClient Client { get; } = new HttpClient {BaseAddress = new Uri("http://127.0.0.1:5984/filehost/")};

        public ObservableCollection<ItemPreviewVM> Items { get; } = new ObservableCollection<ItemPreviewVM>();

        public DelegateCommand UploadFileCommand { get; }
        public DelegateCommand CreateFolderCommand { get; }
        public DelegateCommand WindowLoadCommand { get; set; }

        public bool IsEmpty
        {
            get => _isEmpty;
            set
            {
                if(value == _isEmpty) return;
                _isEmpty = value;
                OnPropertyChanged();
            }
        }

        public MainWindowVM()
        {
            CurrentFolder = null;

            UploadFileCommand = new DelegateCommand(Upload);
            CreateFolderCommand = new DelegateCommand(CreateFolder);
            WindowLoadCommand = new DelegateCommand(WindowLoaded);
        }

        private async void WindowLoaded(object obj)
        {
            switch (obj)
            {
                case FolderPreviewVM folderPreviewVM:
                    CurrentFolder = new FolderItem
                    {
                        Id = folderPreviewVM.Id,
                        Name = folderPreviewVM.Name,
                        Revision = folderPreviewVM.Revision
                    };
                    break;
                case null:
                    CurrentFolder = null;
                    break;
            }

            Items.Clear();

			await LoadItems();
			UpdateIsEmpty();
		}

		private void UpdateIsEmpty()
		{
			IsEmpty = !Items.Any();
		}

		private async Task LoadItems()
        {
            if (IsMainFolder)
            {
                await LoadFolders();
            }
            else
            {
                await LoadFolderFiles();
            }
		}

        private async Task LoadFolderFiles()
        {
            var filesResult = await Client.GetAsync($"_design/filehost/_view/docsByFolder?key=\"{CurrentFolder.Id}\"&include_docs=true");

            if (!filesResult.IsSuccessStatusCode)
            {
                MessageBox.Show($"Error: Could not sync {CurrentFolder.Name}'s files.");
                return;
            }

            var filesJson = await filesResult.Content.ReadAsStringAsync();
            var filesItems = GetFolderFiles(filesJson);

            filesItems.ForEach(Items.Add);
        }

        private List<FilePreviewVM> GetFolderFiles(string filesJson)
        {
            var filesItems = new List<FilePreviewVM>();
            var jFilesItems = JArray.Parse(JObject.Parse(filesJson)["rows"].ToString()).ToList();

            foreach (var fileItemDoc in jFilesItems)
            {
                var fileItem = JsonConvert.DeserializeObject<FileItem>(fileItemDoc["doc"].ToString());
                filesItems.Add(new FilePreviewVM(fileItem));
            }

            return filesItems;
        }

        private async Task LoadFolders()
        {
            var foldersResult = await Client.GetAsync("_design/filehost/_view/folders?include_docs=true");

            if (!foldersResult.IsSuccessStatusCode)
            {
                MessageBox.Show("Error: Could not sync folders.");
                return;
            }

            var foldersJson = await foldersResult.Content.ReadAsStringAsync();
            var folderItems = await GetFolderItems(foldersJson);

            folderItems.ForEach(Items.Add);
        }

        private async Task<List<FolderPreviewVM>> GetFolderItems(string foldersJson)
        {
            var folderItems = new List<FolderPreviewVM>();
            var jFolderItems = JArray.Parse(JObject.Parse(foldersJson)["rows"].ToString()).ToList();

            foreach (var folderItemDoc in jFolderItems)
            {
                var folderItem = JsonConvert.DeserializeObject<FolderItem>(folderItemDoc["doc"].ToString());
                folderItem.ItemsAmount = await GetFolderItemsAmount(folderItem.Id);
                folderItems.Add(new FolderPreviewVM(folderItem));
            }

            return folderItems;
        }

        private async Task<int> GetFolderItemsAmount(string folderId)
        {
            var amountResult = await Client.GetAsync($"_design/filehost/_view/amountOfFolderDocsByFolder?key=\"{folderId}\"");
            var jObjResult = JObject.Parse(await amountResult.Content.ReadAsStringAsync());

            if (!jObjResult["rows"].HasValues) return 0;

            var jObjAmount = jObjResult["rows"][0]["value"];

            return int.Parse(jObjAmount.ToString());
        }

        private async void CreateFolder()
        {
            var folderName = GetFolderName();
            if(folderName == string.Empty) return;

            try
            {
                var folderItem = new FolderItem
                {
                    Name = folderName,
                };

                var docResult = await Client.PostAsync(string.Empty, new StringContent(JsonConvert.SerializeObject(folderItem), Encoding.UTF8, "application/json"));

                if (!docResult.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Error: Some error occurred.\nFolder: {folderItem.Name}");
                    return;
                }

                var doc = JsonConvert.DeserializeAnonymousType(await docResult.Content.ReadAsStringAsync(), new { Id = string.Empty, Rev = string.Empty });
                folderItem.Id = doc.Id;
                folderItem.Revision = doc.Rev;

                Items.Add(new FolderPreviewVM(folderItem));

				UpdateIsEmpty();

			}
            catch (Exception)
            {
                MessageBox.Show($"Error: Some error occurred, could not create folder.\nFolder: {folderName}");
            }
        }

        private string GetFolderName()
        {
            var existingFoldersNames = GetExistingFoldersNames();
            bool isNameSelected;

            do
            {
                var dialog = new FolderNameWindow();
                var dialogResult = dialog.ShowDialog();
                isNameSelected = dialogResult ?? false;

                if (isNameSelected)
                {
                    var name = (dialog.DataContext as FolderNameWindowVM)?.FolderName;
                    var alreadyExist = existingFoldersNames.Any(x => string.CompareOrdinal(x, name) == 0);

                    if (!alreadyExist)
                    {
                        return name;
                    }

                    MessageBox.Show("Error: Folder with the same name already exist.");
                }

            } while (isNameSelected);


            return string.Empty;
        }

        private List<string> GetExistingFoldersNames()
        {
            return Items.Where(x => x is FolderPreviewVM).Select(x => x.Name).ToList();
        }

        private async void Upload()
        {
            var files = GetUploadingFiles();
            if (files == null) return;

            foreach (var file in files)
            {
                var fileItem = await CreateFileDocument(file);
                
                if (fileItem != null)
                {
                    Items.Add(new FilePreviewVM(fileItem));
                    UpdateIsEmpty();
                }
            }

			MessageBox.Show("Upload finished.");
        }

        private IEnumerable<string> GetUploadingFiles()
        {
            var fileDialog = new OpenFileDialog { Multiselect = true };
            var isFilesSelected = fileDialog.ShowDialog();

            return isFilesSelected != null && isFilesSelected == true ? fileDialog.FileNames : null;
        }

        private async Task<FileItem> CreateFileDocument(string file)
        {
            var fileName = Path.GetFileName(file);
            fileName = RenameFileWithExistingName(fileName);

            try
            {
                var bin = ReadBytes(file);

                var fileItem = new FileItem
                {
                    Data = bin,
                    Name = fileName,
                    ContainingFolderId = CurrentFolder?.Id ?? "null",
					Size = bin.Length
                };

                var docResult = await Client.PostAsync(string.Empty, new StringContent(JsonConvert.SerializeObject(fileItem), Encoding.UTF8, "application/json"));

                if (!docResult.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Error: Some error occurred.\nFile: {fileItem.Name}");
                    return null;
                }

                var doc = JsonConvert.DeserializeAnonymousType(await docResult.Content.ReadAsStringAsync(), new {Id = string.Empty, Rev = string.Empty});
                fileItem.Id = doc.Id;
                fileItem.Revision = doc.Rev;

                if (!await UploadDocumentAttachment(fileItem)) return null;

                return fileItem;
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Error: File not found.\nFile: {fileName}");
                return null;
            }
            catch (Exception ex) when(ex is IOException || ex is NotSupportedException)
            {
                MessageBox.Show($"Error: Could not read the file.\nFile: {fileName}");
                return null;
            }
            catch (HttpRequestException)
            {
                MessageBox.Show($"Error: Could not upload file.\nFile: {fileName}");
                return null;
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show($"Error: Could not upload file bigger than 2GB.\nFile: {fileName}");
                return null;
            }
            catch (Exception)
            {
                MessageBox.Show($"Error: Some error occurred.\nFile: {fileName}");
                return null;
            }
        }

        private string RenameFileWithExistingName(string fileName)
        {
            var existingFilesNames = Items.Where(x => x is FilePreviewVM).Select(x => x.Name).ToList();
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            if (existingFilesNames.Any(x => string.CompareOrdinal(x, nameWithoutExtension) == 0))
            {
                return $"{nameWithoutExtension} - {DateTime.Now.Ticks.ToString()}{Path.GetExtension(fileName)}";
            }

            return fileName;
        }

        private byte[] ReadBytes(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private async Task<bool> UploadDocumentAttachment(FileItem fileItem)
        {
            var attachmentResult = await Client.PutAsync($"{fileItem.Id}/{fileItem.Name}?rev={fileItem.Revision}", new ByteArrayContent(fileItem.Data));
            
            if (attachmentResult.IsSuccessStatusCode)
            {
                var content = await attachmentResult.Content.ReadAsStringAsync();
                var rev = JObject.Parse(content)["rev"].ToString();
                fileItem.Revision = rev;

                return true;
            }

            await DeleteDocument(fileItem);
            MessageBox.Show($"Error: Some error occurred.\nFile: {fileItem.Name}");

            return false;
        }

        private async Task DeleteDocument(Item fileItem)
        {
            await Client.DeleteAsync($"{fileItem.Id}/{fileItem.Name}?rev={fileItem.Revision}");

			UpdateIsEmpty();
		}
    }
}

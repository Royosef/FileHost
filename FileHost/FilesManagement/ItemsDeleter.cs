using System.Threading.Tasks;
using FileHost.Models;

namespace FileHost.FilesManagement
{
    public class ItemsDeleter
    {
        private DataAccess DataAccess { get; } = DataAccess.Instance;
        private ItemsLoader ItemsLoader { get; } = new ItemsLoader();

        public async Task DeleteItem(Item item)
        {
            await DataAccess.Delete($"{item.Id}?rev={item.Revision}");
        }

        public async Task DeleteFolder(FolderItem folderItem)
        {
            await DeleteItem(folderItem);

            var fileItems = await ItemsLoader.GetFolderFiles(folderItem);

            foreach (var file in fileItems)
            {
                await DeleteItem(file);
            }
        }
    }
}

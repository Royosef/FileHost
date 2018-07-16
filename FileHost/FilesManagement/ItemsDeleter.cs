using System.Collections.Generic;
using System.Linq;
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
            await DataAccess.Delete(item.Id, item.Revision);
        }

        public async Task DeleteFolder(FolderItem folderItem)
        {
            await DeleteMultipleItems(new List<Item>{folderItem});
        }

        public async Task DeleteMultipleItems(List<Item> items)
        {
            var toDelete = items.Select(x => new { _deleted = true, _id = x.Id, _rev = x.Revision }).ToList();

            foreach (var item in items)
            {
                if (item is FolderItem folderItem)
                {
                    var folerFiles = await ItemsLoader.GetFolderFiles(folderItem);
                    toDelete.AddRange(folerFiles.Select(x => new { _deleted = true, _id = x.Id, _rev = x.Revision }));
                }
            }

            await DataAccess.PostAsJson(new { docs = toDelete }, "_bulk_docs");
        }
    }
}

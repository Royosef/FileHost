using System.Threading.Tasks;
using FileHost.Models;

namespace FileHost.FilesManagement
{
    public class ItemsDeleter
    {
        private DataAccess DataAccess { get; } = new DataAccess();

        public async Task DeleteItem(Item item)
        {
            await DataAccess.Delete($"{item.Id}?rev={item.Revision}");
        }
    }
}

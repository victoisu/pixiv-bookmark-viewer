using System.Collections.Generic;

namespace PixivBookmarkViewer.Data.Works.Interfaces
{
    public interface IWorkPublicTags : IWorkTags
    {
        public IEnumerable<string> PublicTags { get; }
    }
}

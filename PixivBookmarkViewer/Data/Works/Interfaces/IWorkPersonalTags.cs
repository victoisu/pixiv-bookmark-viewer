using System.Collections.Generic;

namespace PixivBookmarkViewer.Data.Works.Interfaces
{
    public interface IWorkPersonalTags : IWorkTags
    {
        public IEnumerable<string> PersonalTags { get; }
    }
}

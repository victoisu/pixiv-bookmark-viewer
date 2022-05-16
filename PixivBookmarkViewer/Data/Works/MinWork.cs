using PixivBookmarkViewer.Data.Works.Interfaces;
using System;

namespace PixivBookmarkViewer.Data.Pixiv
{
    public abstract record MinWork : IWork
    {
        public int Id { get; init; }

        public string Title { get; init; }

        public int IllustType { get; init; }

        public int UserId { get; init; }

        public string UserName { get; init; }
        public long BookmarkId { get; init; }

        public int Width { get; init; }

        public int Height { get; init; }

        public int PageCount { get; init; }

        public bool IsBookmarkable { get; init; }

        public bool IsUnlisted { get; init; }

        public DateTime CreateDate { get; init; }

        public DateTime UpdateDate { get; init; }


        public abstract IWorkDatabase ToDatabaseWork();
    }
}

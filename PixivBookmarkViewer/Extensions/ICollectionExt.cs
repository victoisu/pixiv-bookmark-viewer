using PixivBookmarkViewer.Data;
using System.Collections.Generic;

namespace PixivBookmarkViewer.Extensions
{
	public static class ICollectionExt
	{
		public static WorkCollection ToWorkCollection(this ICollection<FullWork> enumerable)
		{
			var collection = new WorkCollection();
			collection.AddWorks(enumerable);
			return collection;
		}
	}
}

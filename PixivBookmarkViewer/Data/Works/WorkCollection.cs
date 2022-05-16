using PixivBookmarkViewer.Search;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PixivBookmarkViewer.Data
{
    public class WorkCollection : ICollection<FullWork>
	{
		private Dictionary<int, FullWork> _works = new();
		private List<FullWork> _workList = new();

		public IEnumerable<(string, bool)> Tags { get
            {
                var tags = _works.Values.Select(w => (w.PublicTags, true))
            .Union(_works.Values.Select(w => (w.PersonalTags, false)));
				HashSet<(string, bool)> seen = new();

                foreach (var (tagArray, isPublic) in tags)
                {
                    foreach (var tag in tagArray)
                    {
						if (seen.Add((tag, isPublic)))
							yield return (tag, isPublic);
                    }
                }
				yield break;
            }
        }

		public WorkCollection() { }

		public FullWork this[int index] { get => _works[index]; set => _works[value.Id] = value; }

		public void Add(FullWork item)
		{
			_works[item.Id] = item;
		}

		public void AddWorks(IEnumerable<FullWork> works)
		{
			foreach (var work in works)
			{
				_works[work.Id] = work;
			}
			_workList = _works.Values.OrderByDescending(w => w.BookmarkId).ToList();
		}

		public IEnumerable<FullWork> SearchWorks(string searchString)
		{
			var search = Parser.Parse(searchString);
			return _workList.Where(w => w.IsBookmarkable && search.Matches(w));
		}

		#region Interface Implementations
		public int Count => _works.Count;

		public bool IsReadOnly => false;

		public void Clear()
		{
			_works.Clear();
		}

		public bool Contains(FullWork item) => _workList.Contains(item);

		public void CopyTo(FullWork[] array, int arrayIndex)
		{
			foreach (var work in _workList)
			{
				array[arrayIndex++] = work;
			}
		}

		public IEnumerator<FullWork> GetEnumerator() => _workList.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _workList.GetEnumerator();
		}

		public bool Remove(FullWork item)
		{
			return _works.Remove(item.Id) && _workList.Remove(item);
		}
		#endregion
	}
}

using PixivBookmarkViewer.Search.Logic;
using System.Collections.Generic;
using static PixivBookmarkViewer.Search.Logic.ISearchTerm;

namespace PixivBookmarkViewer.Data.Works.Interfaces
{
    public interface IWorkTags : IWork
    {

        public IEnumerable<Tag> Tags { get; }

        public bool HasTag(WordTerm term)
        {
            bool isPartial = term.Attributes.HasFlag(AttributeFlags.Partial) || 
                !term.Attributes.HasFlag(AttributeFlags.Exact);
            bool unsetPubicPersonal = !term.Attributes.HasFlag(AttributeFlags.Personal) && !(term.Attributes.HasFlag(AttributeFlags.Public));
            foreach (var tag in Tags)
            {
                var nameTest = isPartial ? tag.Name.Contains(term.Term) : tag.Name == term.Term;
                var publicTest = unsetPubicPersonal || 
                    (term.Attributes.HasFlag(AttributeFlags.Personal) && !tag.IsPublic) ||
                    (term.Attributes.HasFlag(AttributeFlags.Public) && tag.IsPublic);

                if (nameTest && publicTest)
                    return true;
            }

            return false;
        }
    }
}

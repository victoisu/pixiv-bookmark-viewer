using PixivBookmarkViewer.Data;
using PixivBookmarkViewer.Data.Works.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixivBookmarkViewer.Search.Logic
{
	public interface ISearchTerm
    {
        [Flags]
        public enum AttributeFlags
        {
            None = 0,
            Partial,
            Exact,
            Personal,
            Public
        }

        public HashSet<ISearchTerm> Children { get; }

        public bool IsDisjunctive { get; }

        public bool IsConjunctive { get; }

        public string Readable { get; }

        public bool IsNegated { get; set; }

        public AttributeFlags Attributes { get; set; }

        public bool Add(ISearchTerm other);

        public bool Matches(IWorkTags work);

        public ISearchTerm Apply(FullWork work);

        public ISearchTerm AsOrTerm();

        public ISearchTerm AsAndTerm();

        public ISearchTerm DisjunctiveNormal();

        public ISearchTerm ConjunctiveNormal();

        public ISearchTerm Simplified();

        public ISearchTerm Flatten();

        public ISearchTerm FlippedAndOr();

        public ISearchTerm FlippedNegation();

        public static bool PossibleTags(ISearchTerm term, FullWork work, out HashSet<Tag> includedTags, out HashSet<Tag> excludedTags)
        {
            includedTags = new();
            excludedTags = new();

            var application = term.ConjunctiveNormal().Apply(work);
            if(application is BoolTerm t)
            {
                return false;
            }

            application = application.ConjunctiveNormal();
            foreach (var orBlock in application.Children)
            {
                foreach (var orTerm in orBlock.Children)
                {
                    if (orTerm is FreeTerm simple)
                    {
                        bool includePublic = simple.Attributes.HasFlag(AttributeFlags.Public) 
                            || !simple.Attributes.HasFlag(AttributeFlags.Personal);
                        bool includePersonal = simple.Attributes.HasFlag(AttributeFlags.Personal) 
                            || !simple.Attributes.HasFlag(AttributeFlags.Public);

                        var tags = work.Tags.Where(t => (t.IsPublic && includePublic) || (!t.IsPublic && includePersonal));
                        foreach (var tag in tags)
                        {
                            (orTerm.IsNegated ? excludedTags : includedTags).Add(tag);
                        }
                    }
                }
            }

            foreach (var tag in includedTags)
            {
                if (excludedTags.Contains(tag))
                {
                    includedTags.Remove(tag);
                    excludedTags.Remove(tag);
                }
            }

            return true;
        }
    }
}

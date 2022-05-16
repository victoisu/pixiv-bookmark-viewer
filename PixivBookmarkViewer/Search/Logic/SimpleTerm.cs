using PixivBookmarkViewer.Data.Works.Interfaces;
using System;
using System.Collections.Generic;
using static PixivBookmarkViewer.Search.Logic.ISearchTerm;

namespace PixivBookmarkViewer.Search.Logic
{
    public abstract class SimpleTerm : ISearchTerm
    {
        private HashSet<ISearchTerm> _terms;
        public HashSet<ISearchTerm> Children
        {
            get
            {
                if (_terms == null)
                {
                    _terms = new HashSet<ISearchTerm> { this };
                }

                return _terms;
            }
        }

        public bool IsDisjunctive => true;

        public bool IsConjunctive => true;

        public abstract string Readable { get; }

        public bool IsNegated { get; set; }

        public AttributeFlags Attributes { get; set; }

        public bool Add(ISearchTerm other)
        {
            throw new InvalidOperationException($"Cannot add a child to a {this.GetType().Name}");
        }

        public abstract ISearchTerm Apply(FullWork work);

        public ISearchTerm AsAndTerm() => BlockTerm.MakeAndTerm(this);

        public ISearchTerm AsOrTerm() => BlockTerm.MakeOrTerm(this);

        public ISearchTerm ConjunctiveNormal() => AsOrTerm().AsAndTerm();

        public ISearchTerm DisjunctiveNormal() => AsAndTerm().AsOrTerm();

        public ISearchTerm Flatten() => this;

        public ISearchTerm FlippedAndOr() => this;

        public abstract ISearchTerm FlippedNegation();

        public abstract bool Matches(IWorkTags work);

        public ISearchTerm Simplified() => this;
    }
}

using PixivBookmarkViewer.Data.Works.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using static PixivBookmarkViewer.Search.Logic.ISearchTerm;

namespace PixivBookmarkViewer.Search.Logic
{
	public enum BlockType
    {
        AndBlock,
        OrBlock
    }

    public class BlockTerm : ISearchTerm
    {

        public static ISearchTerm MakeOrTerm(params ISearchTerm[] children)
        {
            return new BlockTerm(BlockType.OrBlock, children);
        }

        public static ISearchTerm MakeOrTerm(IEnumerable<ISearchTerm> children, bool negated = false)
        {
            return new BlockTerm(BlockType.OrBlock, children, negated);
        }

        public static ISearchTerm MakeAndTerm(params ISearchTerm[] children)
        {
            return new BlockTerm(BlockType.AndBlock, children);
        }

        public static ISearchTerm MakeAndTerm(IEnumerable<ISearchTerm> children, bool negated = false)
        {
            return new BlockTerm(BlockType.AndBlock, children, negated);
        }

		public BlockType Type
		{
            get; set;
		}

		HashSet<ISearchTerm> ISearchTerm.Children => Children;

        public virtual HashSet<ISearchTerm> Children { get; protected set; }

        public bool IsDisjunctive
        {
            get
            {
                if (IsNegated)
                    return false;

                switch (Type)
                {
                    case BlockType.AndBlock:
                        return Children.All(c => c is WordTerm);
                    case BlockType.OrBlock:
                        return Children.All(c => (c is WordTerm) || ((c is BlockTerm t) && (t.Type == BlockType.AndBlock) && (t.IsDisjunctive)));
                    default:
                        throw new ArgumentOutOfRangeException("Block has no type.");
                }
            }
        }

        //TODO: DRY
        public bool IsConjunctive
        {
            get
            {
                if (IsNegated)
                    return false;

                switch (Type)
                {
                    case BlockType.OrBlock:
                        return Children.All(c => c is WordTerm);
                    case BlockType.AndBlock:
                        return Children.All(c => (c is WordTerm) || ((c is BlockTerm t) && (t.Type == BlockType.OrBlock) && (t.IsConjunctive)));
                    default:
                        throw new ArgumentOutOfRangeException("Block has no type.");
                }
            }
        }

        public bool IsNegated { get; set; }

        public AttributeFlags Attributes { get; set; }

        public string Readable => Type == BlockType.OrBlock ?
        $"{(IsNegated ? "!" : "")}[{string.Join(" OR ", Children.Select(c => c.Readable))}]" :
        $"{(IsNegated ? "!" : "")}({string.Join(" AND ", Children.Select(c => c.Readable))})";

        public BlockTerm(BlockType type, bool negated = false)
        {
            Type = type;
            Children = new();
            IsNegated = negated;
        }

        public BlockTerm(BlockType type, IEnumerable<ISearchTerm> children, bool negated = false)
        {
            Type = type;
            Children = children.ToHashSet();
            IsNegated = negated;
        }

        public ISearchTerm AsOrTerm()
        {
            if (Type == BlockType.OrBlock)
            {
                return this;
            }

            return MakeOrTerm(this);
        }

        public ISearchTerm AsAndTerm()
        {
            if (Type == BlockType.AndBlock)
            {
                return this;
            }

            return MakeAndTerm(this);
        }

        public bool Add(ISearchTerm other)
		{
            if (other is BlockTerm b && b.Type == Type && !b.IsNegated)
			{
                Children.UnionWith(b.Children);
			}
            else
			{
                Children.Add(other);
			}

            return true;
		}

        public bool Matches(IWorkTags work)
		{
            if (Type == BlockType.AndBlock)
			{
                return Children.All(c => c.Matches(work)) ^ IsNegated;
			}
            else
			{
                return Children.Any(c => c.Matches(work)) ^ IsNegated;
			}
        }

        // (a and b) or (c and d)
        protected ISearchTerm DisjunctiveNormalIntern()
        {
            if (IsNegated)
                return Simplified().DisjunctiveNormal();

            ISearchTerm res = Simplified();
            if (res.IsDisjunctive)
                res = MakeOrTerm(res.Children.Select(c => c.AsAndTerm()));
            else
                res = DisjunctiveIntern();

            return res;
        }
        public ISearchTerm DisjunctiveNormal()
        {
            if (IsNegated)
                return Simplified().DisjunctiveNormal();

            if (Type == BlockType.OrBlock)
            {
                return DisjunctiveNormalIntern();
            }
            else
            {
                return (MakeOrTerm(this) as BlockTerm).DisjunctiveNormalIntern();
            }
        }

        // (a or b) and (c or d)
        public ISearchTerm ConjunctiveNormal()
        {
            if (IsNegated)
                return Simplified().ConjunctiveNormal();

            var res = FlippedAndOr();
            res = res.DisjunctiveNormal();
            res = res.FlippedAndOr();
            return res;
        }

        public virtual ISearchTerm Flatten()
		{
            var flatChildren = Children.Select(c => c.Flatten()).Where(c => c.Children.Count > 0).ToHashSet();
            Children = flatChildren;
            if (Children.Count != 1)
                return this;
            return IsNegated ? Children.First().Flatten().FlippedNegation() : Children.First().Flatten();
		}

        public ISearchTerm Simplified()
		{
            return IsNegated ? 
                DeMorgan().Simplified() : 
                new BlockTerm(Type, Children.Select(c => c.Simplified()));
        }

        public ISearchTerm FlippedAndOr()
        {
            return Type == BlockType.OrBlock ?
                MakeAndTerm(Children.Select(c => c.FlippedAndOr()), IsNegated) :
                MakeOrTerm(Children.Select(c => c.FlippedAndOr()), IsNegated);
        }

        protected ISearchTerm DisjunctiveIntern()
        {
            //var simples = Children.Select(c => c is BlockTerm t ? t.DisjunctiveNormalIntern() : c.DisjunctiveNormal()).ToList();
            //List<ISearchTerm> result = new();

            //foreach (var andTerm in simples)
            //{
            //    var grandchildren = andTerm.Children.Select(c => c.Children).ToList();
            //    Combinations(grandchildren).ToList().ForEach(term => result.Add(MakeAndTerm(term)));
            //}

            //return MakeOrTerm(result);

            return MakeOrTerm(
                Children.Select(c => c is BlockTerm t ? t.DisjunctiveNormalIntern() : c.DisjunctiveNormal())
                .SelectMany(s => Combinations(s.Children
                        .Select(c => c.Children)
                    )
                    .Select(term => MakeAndTerm(term))
                )
                );
        }

        public ISearchTerm DeMorgan()
        {
            return Type == BlockType.OrBlock ? 
                MakeAndTerm(Children.Select(c => c.FlippedNegation()), !IsNegated) :
                MakeOrTerm(Children.Select(c => c.FlippedNegation()), !IsNegated);
        }

        public HashSet<HashSet<ISearchTerm>> ChildrenCombinations()
		{
            var resChildren = new HashSet<HashSet<ISearchTerm>>();

            foreach (var child in Children)
            {
                var simple = child.DisjunctiveNormal().Children.Select(c => c.Children);
                resChildren.UnionWith(Combinations(simple));
            }

            return resChildren;
        }

        public static HashSet<HashSet<ISearchTerm>> Combinations(IEnumerable<IEnumerable<ISearchTerm>> other)
		{
            var res = new HashSet<HashSet<ISearchTerm>>();
            if (other.Count() == 0)
            {
                res.Add(new HashSet<ISearchTerm>());
                return res;
            }
            var take = other.First();
            var remain = other.Except(new HashSet<IEnumerable<ISearchTerm>> { take });
            var sub = Combinations(remain);

            foreach (var child in take)
			{
				foreach (var set in sub)
				{
                    res.Add(set.Union(new HashSet<ISearchTerm> { child }).ToHashSet());
				}
			}

            return res;
        }

		public override string ToString() => (IsNegated ? "!" : "") + (Type == BlockType.OrBlock ?
            $"OrTerm[{string.Join(", ", Children.Select(c => c.ToString()))}]" :
            $"AndTerm({string.Join(", ", Children.Select(c => c.ToString()))})");

        public ISearchTerm FlippedNegation()
        {
            return new BlockTerm(Type, Children.Select(c => c), !IsNegated);
        }

        public ISearchTerm Apply(FullWork work)
        {
            return new BlockTerm(Type, Children.Select(c => c.Apply(work)), IsNegated).Resolve();
        }

        public ISearchTerm Resolve()
        {
            var newchildren = new HashSet<ISearchTerm>();
            foreach (var child in Children)
            {
                if ((child == BoolTerm.True && Type == BlockType.OrBlock) || (child == BoolTerm.False && Type == BlockType.AndBlock))
                {
                    return IsNegated ? child.FlippedNegation() : child;
                }
                else { 
                    newchildren.Add(child);
                }
            }

            if (newchildren.Count == 0)
                return IsNegated ? BoolTerm.False : BoolTerm.True;

            var res = new BlockTerm(Type, newchildren, IsNegated);
            res.Attributes = this.Attributes;
            return res.Flatten();
        }
    }
}

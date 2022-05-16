using PixivBookmarkViewer.Data.Works.Interfaces;
using System;
using System.Text.RegularExpressions;

namespace PixivBookmarkViewer.Search.Logic
{
    public class WordTerm : SimpleTerm
	{
		public static readonly WordTerm BlankTerm = new();

		public string Term { get; }
		public override string Readable => $"{(IsNegated ? "!" : "")}{Term}";

		private static readonly Regex BlankRegex = new(@"^\s+$");

		protected WordTerm()
		{
			Term = "";
			IsNegated = false;
		}

		public WordTerm(string word, bool negated = false)
		{
			if (BlankRegex.IsMatch(word))
				throw new ArgumentException("Word cannot be blank!");

			Term = word;
			IsNegated = negated;
		}

        public override bool Matches(IWorkTags work)
		{
			return work != null && (this == BlankTerm || (work.HasTag(this)) ^ IsNegated);
		}

		public override ISearchTerm FlippedNegation() => new WordTerm(Term, !IsNegated);

		public override bool Equals(object obj)
		{
			if (obj is WordTerm other)
			{
				return other.Term == Term && other.IsNegated == IsNegated;
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return Term.GetHashCode();
		}

		public override string ToString() => $"{(IsNegated ? "!" : "")}WordTerm({Term})";

		public override ISearchTerm Apply(FullWork work) => Matches(work) ? BoolTerm.True : BoolTerm.False;
    }
}

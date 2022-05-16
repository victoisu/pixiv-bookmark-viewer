using PixivBookmarkViewer.Search.Logic;
using System;
using System.Text.RegularExpressions;
using static PixivBookmarkViewer.Search.Logic.ISearchTerm;

namespace PixivBookmarkViewer.Search
{
	// Order matters, earlier is higher priority matching.
	public enum Token
	{
		Space,
		OpenParenthesis,
		ClosedParenthesis,
		And,
		Or,
		Not,
		Exact,
		Partial,
		Public,
		Personal,
		Free,
		Word
	}

	public static class Extensions
	{
		public static Regex ToRegex(this Token t)
		{
            switch (t)
            {
                case Token.Space:
                    return Regex(@"\s+");
                case Token.OpenParenthesis:
                    return Regex(@"\(");
                case Token.ClosedParenthesis:
                    return Regex(@"\)");
                case Token.And:
                    return Regex(@"AND");
                case Token.Or:
                    return Regex(@"OR");
                case Token.Not:
                    return Regex(@"NOT");
                case Token.Exact:
                    return Regex(@"EXACT");
                case Token.Partial:
                    return Regex(@"PARTIAL");
				case Token.Public:
					return Regex(@"PUBLIC");
				case Token.Personal:
					return Regex(@"PERSONAL");
				case Token.Free:
					return Regex(@"FREE");
				case Token.Word:
                    return Regex(@"\S+");
                default:
                    throw new ArgumentException($"No regex found for token {t}");
            }
        }

        private static Regex Regex(string regex)
		{
			return new Regex($"^{regex}");
		}
    }

    public class Parser
	{

		public static ISearchTerm Parse(string search)
		{
			return new Parser(search).Parse();
		}

		private string _search;
		private string _current;
		private AttributeFlags _nextAttribs = AttributeFlags.None;
		private bool _negateNext = false;

		public Parser(string search)
		{
			_search = search;
		}

		public ISearchTerm Parse()
		{
			_current = _search;

			return ParseTerm();
		}

		private ISearchTerm ParseTerm()
		{
			ISearchTerm result = BlockTerm.MakeAndTerm();
			while (_current.Length > 0)
			{
				var (token, match, remaining) = MatchTerm(_current);
				_current = remaining;
				switch (token)
				{
					case Token.OpenParenthesis:
						var negated = TakeNegation();
						var attribs = _nextAttribs;
						var sub = ParseTerm();
						sub.IsNegated = negated;
						sub.Attributes = attribs;
						result.Add(sub);
						break;
					case Token.ClosedParenthesis:
						return OutputTerm(result.Flatten());
					case Token.Or:
						var next = BlockTerm.MakeOrTerm();
						ResetAttributes();
						next.Add(result);
						next.Add(ParseTerm());
						return OutputTerm(next);
					case Token.Not:
						_negateNext = !_negateNext;
						break;
					case Token.Exact:
						_nextAttribs |= AttributeFlags.Exact;
						break;
					case Token.Partial:
						_nextAttribs |= AttributeFlags.Partial;
						break;
					case Token.Public:
						_nextAttribs |= AttributeFlags.Public;
						break;
					case Token.Personal:
						_nextAttribs |= AttributeFlags.Personal;
						break;
					case Token.Free:
						var free = new FreeTerm();
						free.Attributes = _nextAttribs;
						free.IsNegated = _negateNext;
						ResetAttributes();
						result.Add(free);
						break;
					case Token.Word:
						var res = new WordTerm(match, TakeNegation());
						res.Attributes = _nextAttribs;
						result.Add(res);
						ResetAttributes();
						break;
					case Token.And:
						ResetAttributes();
						break;
					case Token.Space:
						break;
				}
			}
			return OutputTerm(result);
		}

		private (Token, string, string) MatchTerm(string search)
		{
			foreach (var token in Enum.GetValues<Token>())
			{
				var regex = token.ToRegex();
				var match = regex.Match(search);
				if (match.Success)
				{
					return (token, match.Value, search[match.Value.Length..]);
				}
			}
			throw new InvalidOperationException($"No token found in \"{search}\"?");
		}

		private ISearchTerm OutputTerm(ISearchTerm term)
        {
			term.Attributes = _nextAttribs;
			term.IsNegated = TakeNegation();
			_nextAttribs = AttributeFlags.None;
			return term.Flatten();
        }

		private void ResetAttributes()
		{
			_negateNext = false;
			_nextAttribs = AttributeFlags.None;
		}
		
		private bool TakeNegation()
        {
			var isNegated = _negateNext;
			_negateNext = false;
			return isNegated;
        }
	}
}

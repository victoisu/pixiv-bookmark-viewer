using PixivBookmarkViewer.Data.Works.Interfaces;

namespace PixivBookmarkViewer.Search.Logic
{
    public class BoolTerm : SimpleTerm
    {
        public static readonly BoolTerm True = new(true);
        public static readonly BoolTerm False = new(false);

        public readonly bool Value;
        public override string Readable => Value ? "TRUE" : "FALSE";
        public new bool IsNegated => false;

        protected BoolTerm(bool value)
        {
            Value = value;
        }

        public override ISearchTerm FlippedNegation() => this == True ? False : True;

        public override bool Matches(IWorkTags work) => Value;

        public override bool Equals(object obj)
        {
            if (this == True || this == False)
            {
                return (obj == False || obj == True) && obj != this;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override ISearchTerm Apply(FullWork work) => this;
    }
}

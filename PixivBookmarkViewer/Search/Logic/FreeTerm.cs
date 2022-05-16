using PixivBookmarkViewer.Data.Works.Interfaces;

namespace PixivBookmarkViewer.Search.Logic
{
    public class FreeTerm : SimpleTerm
    {
        public override string Readable => "FREE";

        public override ISearchTerm Apply(FullWork work) => this;

        public override ISearchTerm FlippedNegation() => 
            new FreeTerm { IsNegated = !IsNegated, Attributes = Attributes};

        public override bool Matches(IWorkTags work) => true;
    }
}

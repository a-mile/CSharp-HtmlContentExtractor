using System.Collections.Generic;
using System.Linq;
using CSharp_HtmlContentExtractor_Library.ExtensionMethods;
using CSharp_HtmlParser_Library.HtmlDocumentStructure;

namespace CSharp_HtmlContentExtractor_Library
{
    public class HtmlContentExtractor
    {
        public string Output { get; private set; }
        public int ArticleThreshold { get; set; } = 10;
        public double LinkRatioThreshold { get; set; } = 0.5;

        private readonly string _pageHtml;
        private readonly string _pageUrl;
        private HtmlDocument _htmlDocument;
        private IEnumerable<HtmlDocumentNode> _pageNodes;
        private IList<HtmlDocumentNode> _textNodes;
        private IList<HtmlDocumentNode> _textNodesParents;
        private HtmlDocumentNode _parentWithHighestWordCount;
        private string _title;

        private readonly List<string> _reduntandTagsNames = new List<string>
        {
            "script","style","head"
        };
        private readonly List<string> _permittedTagNames = new List<string>
        {
            "article", "div", "span", "section", "tr", "td", "h1", "h2", "h3", "h4", "h5", "h6", "h7", "li", "ul", "ol"
        };

        public HtmlContentExtractor(string pageHtml)
        {
            _pageHtml = pageHtml;
        }
        public HtmlContentExtractor(string pageHtml, string pageUrl) : this(pageHtml)
        {
            _pageUrl = pageUrl;
        }
        public void ExtractContent()
        {
            ParsePage();
            GetPageTitle();
            RemoveReduntandNodes();
            GetPageNodes();
            GetTextNodes();
            GetTextNodesParents();
            //RemoveParentsBelowArticleThreshold();
            RemoveLinks();
            MakeTextNodesParentsPermittedTags();
            HoldDivsOnly();
            //MakeTextNodesParentsHighestPossible();
            RemoveNodesWithSameParent();
            KeepLowest();
            MergeDivs();                    
            MakeTextNodesParentsHighestPossible(); 
            RemoveNodesWithSameParent();            
            RemoveComments();

            GetParentWithHighestWordCount();
            //if (_pageUrl != string.Empty)
                //CorrectLinks();
            //CleanParent();

            //Output += System.IO.File.ReadAllText("header");
            Output += "<body style='width: 800px; margin: auto; text-align: justify;'>";
            if(_title != string.Empty)
                Output += "<h1>"+_title+"</h1>";
            Output += _parentWithHighestWordCount.OuterHtml;   
            Output += "</body>";   
            Output += "</html>";

            /*int c = 1;
            foreach (var n in _textNodesParents)
            {
                Output += "####NODE###" + c + " " + n.OwnHtml + "\n";
                //Output += n.InnerText + "\n";
                c++;
            }*/
        }

        private void KeepLowest()
        {
            var newTextNodesParents = new List<HtmlDocumentNode>();

            foreach (var parent in _textNodesParents)
            {
                bool correct = true;
                var descendants = parent.Descendants;

                foreach (var remainingParent in _textNodesParents)
                {
                    if (descendants.Contains(remainingParent))
                    {
                        correct = false;
                    }
                }

                if(correct)
                    newTextNodesParents.Add(parent);
            }
            _textNodesParents = newTextNodesParents;
        }
        private void ParsePage()
        {
            _htmlDocument = new HtmlDocument(_pageHtml);
            _htmlDocument.Parse();
        }
        private void GetPageTitle()
        {
            HtmlDocumentNode title = _htmlDocument.RootNode.Descendants.FirstOrDefault(x => x.Name == "title");
            _title = title != null ? title.OwnText : string.Empty;
        }
        private void RemoveReduntandNodes()
        {
            List<HtmlDocumentNode> nodesToDelete = new List<HtmlDocumentNode>();

            foreach (var tagName in _reduntandTagsNames)
            {
                nodesToDelete.AddRange(_htmlDocument.RootNode.Descendants.Where(x => x.Name == tagName).ToList());
            }
            nodesToDelete.AddRange(_htmlDocument.RootNode.Descendants.Where(x => x.Flags.Contains(Flags.SpecialTag)).ToList());

            foreach (var node in nodesToDelete)
            {
                _htmlDocument.RootNode.DeleteNode(node);
            }
        }
        private void GetPageNodes()
        {
            _pageNodes = _htmlDocument.RootNode.Descendants;
        }
        private void GetTextNodes()
        {
            _textNodes = _pageNodes.Where(x => x.Flags.Contains(Flags.Text) && x.OwnText.WordCount() > ArticleThreshold).OrderBy(x => x.Position).ToList();
        }
        private void GetTextNodesParents()
        {
            _textNodesParents = _textNodes.Select(x => x.ParentNode).Distinct().ToList();
        }
        private void RemoveParentsBelowArticleThreshold()
        {
            List<HtmlDocumentNode> newParents = new List<HtmlDocumentNode>();
            foreach (var parent in _textNodesParents)
            {
                if (parent.InnerText.WordCount() > ArticleThreshold)
                    newParents.Add(parent);
            }
            _textNodesParents = newParents;
        }
        private void RemoveLinks()
        {
            List<HtmlDocumentNode> newTextNodesParents = new List<HtmlDocumentNode>();
            foreach (var parent in _textNodesParents)
            {
                int wordCount = parent.InnerText.WordCount();
                int linkWordCount = 0;

                var textNodes = parent.Descendants.Where(x => x.Flags.Contains(Flags.Text));

                foreach (var node in textNodes)
                {
                    HtmlDocumentNode textNodeParent = node.ParentNode;

                    while (textNodeParent != null && textNodeParent.Name != "a")
                        textNodeParent = textNodeParent.ParentNode;
                    if (textNodeParent != null)
                        linkWordCount += node.OwnText.WordCount();
                }

                double ratio = (double)linkWordCount / wordCount;
                if (ratio < LinkRatioThreshold)
                    newTextNodesParents.Add(parent);
            }

            _textNodesParents = newTextNodesParents;
        }        
        private void MakeTextNodesParentsPermittedTags()
        {
            for (int i = 0; i < _textNodesParents.Count; i++)
            {
                while (_textNodesParents[i].ParentNode != null && !_permittedTagNames.Contains(_textNodesParents[i].Name))
                    _textNodesParents[i] = _textNodesParents[i].ParentNode;
                while( _textNodesParents[i].ParentNode != null && _textNodesParents[i].ParentNode.InnerText == _textNodesParents[i].InnerText)
                    _textNodesParents[i] = _textNodesParents[i].ParentNode;
            }
        }
        private void HoldDivsOnly()
        {
            _textNodesParents = _textNodesParents.Where(x => x.Name == "div").ToList();
        }
        private void MergeDivs()
        {
            /*List<HtmlDocumentNode> multipleParents = _textNodesParents.GroupBy(x => x.ParentNode).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            List<string> multipleOwns = _textNodesParents.GroupBy(x => x.OwnHtml).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            for (int i = 0; i < _textNodesParents.Count; i++)
            {
                if (multipleParents.Contains(_textNodesParents[i].ParentNode))// && multipleOwns.Contains(_textNodesParents[i].OwnHtml))
                    _textNodesParents[i] = _textNodesParents[i].ParentNode;
            }*/
            for(int i=0; i<_textNodesParents.Count; i++)
            {
                var sameParents = _textNodesParents
                    .Where(x => x.OwnHtml == _textNodesParents[i].OwnHtml && x.ParentNode == _textNodesParents[i].ParentNode);
                if (sameParents.Count() > 1)
                {
                    _textNodesParents[i] = _textNodesParents[i].ParentNode;
                }
            }
        }
        private void MakeTextNodesParentsHighestPossible()
        {
            List<HtmlDocumentNode> parents = _textNodesParents.Select(x => x).Distinct().ToList();

            foreach (var parent in parents)
            {
                for (int i = 0; i < _textNodesParents.Count; i++)
                {
                    HtmlDocumentNode textNodeParent = _textNodesParents[i];

                    while (textNodeParent != null && textNodeParent != parent)
                        textNodeParent = textNodeParent.ParentNode;

                    if (textNodeParent != null)
                        _textNodesParents[i] = textNodeParent;
                }
            }
        }
        private void RemoveNodesWithSameParent()
        {
            _textNodesParents = _textNodesParents.Distinct().OrderBy(x => x.Position).ToList();
        }
        private void RemoveComments()
        {
            List<string> multipleParents = _textNodesParents.GroupBy(x => x.OwnHtml).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            List<HtmlDocumentNode> newTextNodesParents = new List<HtmlDocumentNode>();
            foreach (var parent in _textNodesParents)
            {
                if (!multipleParents.Contains(parent.OwnHtml))
                    newTextNodesParents.Add(parent);
            }
            _textNodesParents = newTextNodesParents;
        }
        private void GetParentWithHighestWordCount()
        {
            int maxWordCount = _textNodesParents.Select(x => x.InnerText.WordCount()).Max();
            _parentWithHighestWordCount = _textNodesParents.FirstOrDefault(x => x.InnerText.WordCount() == maxWordCount);
        }
        private void CorrectLinks()
        {
            string source = string.Empty;
            bool thirdSlash = _pageUrl.Substring(0, 7) == "http://" || _pageUrl.Substring(0, 8) == "https://";
            var slashesRemain = thirdSlash ? 3 : 1;
            foreach (var character in _pageUrl)
            {
                if (character == '/')
                    slashesRemain--;
                if (slashesRemain == 0)
                    break;
                source += character;
            }

            foreach (var node in _parentWithHighestWordCount.Descendants)
            {
                if (node.Name == "a")
                {
                    HtmlAttribute href = node.Attributes.FirstOrDefault(x => x.Name == "href");
                    if (href != null)
                    {
                        if (href.Value[0] == '/')
                            href.Value = source + href.Value;
                    }
                }
                if (node.Name == "img")
                {
                    HtmlAttribute src = node.Attributes.FirstOrDefault(x => x.Name == "src");
                    if (src != null)
                    {
                        if (src.Value[0] == '/')
                            src.Value = source + src.Value;
                    }
                }
            }
        }
        private void CleanParent()
        {
            List<HtmlDocumentNode> nodesToDelete = new List<HtmlDocumentNode>();
            foreach (var node in _parentWithHighestWordCount.Descendants)
            {
                if (node.Name != "a" && node.Name != "img")
                    node.Attributes.Clear();
                if (node.Name == "a")
                {
                    HtmlAttribute href = node.Attributes.FirstOrDefault(x => x.Name == "href");
                    if (href != null)
                    {
                        if (href.Value.Contains("facebook.com"))
                        {
                            HtmlDocumentNode aParent = node.ParentNode;
                            while (aParent != null && aParent.Name != "div" && aParent.Name != "span" && aParent.Name != "section")
                                aParent = aParent.ParentNode;
                            if (aParent != null)
                                nodesToDelete.Add(aParent);
                        }
                    }
                }
                if (node.Name == "button")
                {
                    HtmlDocumentNode aParent = node.ParentNode;
                    while (aParent != null && aParent.Name != "div" && aParent.Name != "span" && aParent.Name != "section")
                        aParent = aParent.ParentNode;
                    if (aParent != null)
                        nodesToDelete.Add(aParent);
                }
            }
            foreach (var node in nodesToDelete)
            {
                _parentWithHighestWordCount.DeleteNode(node);
            }
        }
    }
}
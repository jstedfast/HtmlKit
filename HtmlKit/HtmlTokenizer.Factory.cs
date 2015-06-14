
using System.Text;
namespace HtmlKit
{
    
    partial class HtmlTokenizer
    {
        HtmlToken CreateCommentToken(string comment)
        {
            return tokenFactory.CreateCommentToken(comment);
        }
        HtmlToken CreateCommentToken(StringBuilder stbuilder)
        {
            return tokenFactory.CreateCommentToken(stbuilder.ToString());
        }
        HtmlToken CreateDataToken(string data)
        {
            return tokenFactory.CreateHtmlDataToken(data);
        }
        HtmlToken CreateDataToken(StringBuilder stbuilder)
        {
            return tokenFactory.CreateHtmlDataToken(stbuilder.ToString());
        }
        HtmlDocTypeToken CreateDocTypeToken(bool forceQuirksMode = false)
        {
            HtmlDocTypeToken tk = (HtmlDocTypeToken)tokenFactory.CreateHtmlDocTypeToken();
            tk.ForceQuirksMode = forceQuirksMode;
            return tk;
        }
        HtmlTagToken CreateTagToken(string name, bool isEndTag)
        {
            return (HtmlTagToken)tokenFactory.CreateHtmlTagToken(name, isEndTag);
        }
        HtmlTagToken CreateTagToken(StringBuilder stbuilder, bool isEndTag)
        {
            return (HtmlTagToken)tokenFactory.CreateHtmlTagToken(stbuilder.ToString(), isEndTag);
        }
    }
}
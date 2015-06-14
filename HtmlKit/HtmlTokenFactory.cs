//2015, MIT, WinterDev

using System;

namespace HtmlKit
{
    public abstract class HtmlTokenFactory
    {
        public abstract HtmlToken CreateCommentToken(string comment);
        public abstract HtmlToken CreateHtmlDataToken(string data);
        public abstract HtmlToken CreateHtmlTagToken(string name, bool isEndTag);
        public abstract HtmlToken CreateHtmlDocTypeToken();

    }

    public class MyHtmlTokenFactory : HtmlTokenFactory
    {
        public override HtmlToken CreateCommentToken(string comment)
        {
            return new HtmlCommentToken(comment);
        }

        public override HtmlToken CreateHtmlDataToken(string data)
        {
            return new HtmlDataToken(data);
        }

        public override HtmlToken CreateHtmlDocTypeToken()
        {
            return new HtmlDocTypeToken();
        }

        public override HtmlToken CreateHtmlTagToken(string name, bool isEndTag)
        {
            return new HtmlTagToken(name, isEndTag);
        }
    }

}
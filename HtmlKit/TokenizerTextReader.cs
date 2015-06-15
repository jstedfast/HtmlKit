//2015, WinterDev
using System.IO;
using System.Text;

namespace HtmlKit
{
    public class TokenizerTextReader
    {
        TextReader textReader;
        int currentLine;
        int currentColumn;
        public TokenizerTextReader(TextReader textReader)
        {
            this.textReader = textReader;
        }
        public bool Peek(out char c)
        {                
            int peek = textReader.Peek();
            if (peek == -1)
            {
                c = '\0';
                return false;
            }
            else {
                c = (char)peek;
                return true;
            } 
        }
        public int CurrentLine
        {
            get { return currentLine; }
        }
        public int CurrentColumn
        {
            get { return currentColumn; }
        }
        public bool ReadNext()
        {
            char c;
            return ReadNext(out c);               
                
        }
        public bool ReadNext(out char c)
        {
            int next= textReader.Read();
            if(next < 0)
            {
                c = '\0';
                return false;
            }

            c = (char)next;
            switch (c)
            {
                case '\n':
                    currentLine++;
                    currentColumn = 0;
                    break;
                default:
                    currentColumn++;
                    break;
            }
            return true;
        }

    }



}
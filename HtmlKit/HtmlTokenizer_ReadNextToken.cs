//
// HtmlTokenizer.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System.IO;
using System.Text;

namespace HtmlKit
{
    /// <summary>
    /// An HTML tokenizer.
    /// </summary>
    /// <remarks>
    /// Tokenizes HTML text, emitting an <see cref="HtmlToken"/> for each token it encounters.
    /// </remarks>
    partial class HtmlTokenizer
    {
        HtmlToken _nextEmitToken;
        public bool ReadNextToken(out HtmlToken token)
        {
            token = null;
            //1. init
            _nextEmitToken = null;
            //2. 
            while (TokenizerState != HtmlTokenizerState.EndOfFile)
            {

                switch (TokenizerState)
                {
                    case HtmlTokenizerState.Data:
                        ReadDataToken(out token);
                        break;
                    case HtmlTokenizerState.CharacterReferenceInData:
                        ReadCharacterReferenceInData(out token);
                        break;
                    case HtmlTokenizerState.RcData:
                        ReadRcData(out token);
                        break;
                    case HtmlTokenizerState.CharacterReferenceInRcData:
                        ReadCharacterReferenceInRcData(out token);
                        break;
                    case HtmlTokenizerState.RawText:
                        ReadRawText(out token);
                        break;
                    case HtmlTokenizerState.ScriptData:
                        ReadScriptData(out token);
                        break;
                    case HtmlTokenizerState.PlainText:
                        ReadPlainText(out token);
                        break;
                    case HtmlTokenizerState.TagOpen:
                        ReadTagOpen(out token);
                        break;
                    case HtmlTokenizerState.EndTagOpen:
                        ReadEndTagOpen(out token);
                        break;
                    case HtmlTokenizerState.TagName:
                        ReadTagName(out token);
                        break;
                    case HtmlTokenizerState.RcDataLessThan:
                        ReadRcDataLessThan(out token);
                        break;
                    case HtmlTokenizerState.RcDataEndTagOpen:
                        ReadRcDataEndTagOpen(out token);
                        break;
                    case HtmlTokenizerState.RcDataEndTagName:
                        ReadRcDataEndTagName(out token);
                        break;
                    case HtmlTokenizerState.RawTextLessThan:
                        ReadRawTextLessThan(out token);
                        break;
                    case HtmlTokenizerState.RawTextEndTagOpen:
                        ReadRawTextEndTagOpen(out token);
                        break;
                    case HtmlTokenizerState.RawTextEndTagName:
                        ReadRawTextEndTagName(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataLessThan:
                        ReadScriptDataLessThan(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEndTagOpen:
                        ReadScriptDataEndTagOpen(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEndTagName:
                        ReadScriptDataEndTagName(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEscapeStart:
                        ReadScriptDataEscapeStart(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEscapeStartDash:
                        ReadScriptDataEscapeStartDash(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEscaped:
                        ReadScriptDataEscaped(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedDash:
                        ReadScriptDataEscapedDash(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedDashDash:
                        ReadScriptDataEscapedDashDash(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedLessThan:
                        ReadScriptDataEscapedLessThan(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedEndTagOpen:
                        ReadScriptDataEscapedEndTagOpen(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedEndTagName:
                        ReadScriptDataEscapedEndTagName(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapeStart:
                        ReadScriptDataDoubleEscapeStart(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscaped:
                        ReadScriptDataDoubleEscaped(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapedDash:
                        ReadScriptDataDoubleEscapedDash(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapedDashDash:
                        ReadScriptDataDoubleEscapedDashDash(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapedLessThan:
                        ReadScriptDataDoubleEscapedLessThan(out token);
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapeEnd:
                        ReadScriptDataDoubleEscapeEnd(out token);
                        break;
                    case HtmlTokenizerState.BeforeAttributeName:
                        ReadBeforeAttributeName(out token);
                        break;
                    case HtmlTokenizerState.AttributeName:
                        ReadAttributeName(out token);
                        break;
                    case HtmlTokenizerState.AfterAttributeName:
                        ReadAfterAttributeName(out token);
                        break;
                    case HtmlTokenizerState.BeforeAttributeValue:
                        ReadBeforeAttributeValue(out token);
                        break;
                    case HtmlTokenizerState.AttributeValueQuoted:
                        ReadAttributeValueQuoted(out token);
                        break;
                    case HtmlTokenizerState.AttributeValueUnquoted:
                        ReadAttributeValueUnquoted(out token);
                        break;
                    case HtmlTokenizerState.CharacterReferenceInAttributeValue:
                        ReadCharacterReferenceInAttributeValue(out token);
                        break;
                    case HtmlTokenizerState.AfterAttributeValueQuoted:
                        ReadAfterAttributeValueQuoted(out token);
                        break;
                    case HtmlTokenizerState.SelfClosingStartTag:
                        ReadSelfClosingStartTag(out token);
                        break;
                    case HtmlTokenizerState.BogusComment:
                        ReadBogusComment(out token);
                        break;
                    case HtmlTokenizerState.MarkupDeclarationOpen:
                        ReadMarkupDeclarationOpen(out token);
                        break;
                    case HtmlTokenizerState.CommentStart:
                        ReadCommentStart(out token);
                        break;
                    case HtmlTokenizerState.CommentStartDash:
                        ReadCommentStartDash(out token);
                        break;
                    case HtmlTokenizerState.Comment:
                        ReadComment(out token);
                        break;
                    case HtmlTokenizerState.CommentEndDash:
                        ReadCommentEndDash(out token);
                        break;
                    case HtmlTokenizerState.CommentEnd:
                        ReadCommentEnd(out token);
                        break;
                    case HtmlTokenizerState.CommentEndBang:
                        ReadCommentEndBang(out token);
                        break;
                    case HtmlTokenizerState.DocType:
                        ReadDocType(out token);
                        break;
                    case HtmlTokenizerState.BeforeDocTypeName:
                        ReadBeforeDocTypeName(out token);
                        break;
                    case HtmlTokenizerState.DocTypeName:
                        ReadDocTypeName(out token);
                        break;
                    case HtmlTokenizerState.AfterDocTypeName:
                        ReadAfterDocTypeName(out token);
                        break;
                    case HtmlTokenizerState.AfterDocTypePublicKeyword:
                        ReadAfterDocTypePublicKeyword(out token);
                        break;
                    case HtmlTokenizerState.BeforeDocTypePublicIdentifier:
                        ReadBeforeDocTypePublicIdentifier(out token);
                        break;
                    case HtmlTokenizerState.DocTypePublicIdentifierQuoted:
                        ReadDocTypePublicIdentifierQuoted(out token);
                        break;
                    case HtmlTokenizerState.AfterDocTypePublicIdentifier:
                        ReadAfterDocTypePublicIdentifier(out token);
                        break;
                    case HtmlTokenizerState.BetweenDocTypePublicAndSystemIdentifiers:
                        ReadBetweenDocTypePublicAndSystemIdentifiers(out token);
                        break;
                    case HtmlTokenizerState.AfterDocTypeSystemKeyword:
                        ReadAfterDocTypeSystemKeyword(out token);
                        break;
                    case HtmlTokenizerState.BeforeDocTypeSystemIdentifier:
                        ReadBeforeDocTypeSystemIdentifier(out token);
                        break;
                    case HtmlTokenizerState.DocTypeSystemIdentifierQuoted:
                        ReadDocTypeSystemIdentifierQuoted(out token);
                        break;
                    case HtmlTokenizerState.AfterDocTypeSystemIdentifier:
                        ReadAfterDocTypeSystemIdentifier(out token);
                        break;
                    case HtmlTokenizerState.BogusDocType:
                        ReadBogusDocType(out token);
                        break;
                    case HtmlTokenizerState.CDataSection:
                        ReadCDataSection(out token);
                        break;
                    case HtmlTokenizerState.EndOfFile:
                        token = null;
                        return false;
                }


                if ((_nextEmitToken = token) != null)
                {
                    return true;//found next token 
                }
            }
            //3.
            token = null;
            return false;
        }
    }
}
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
        public bool ReadNextToken(out HtmlToken token)
        {
            do
            {
                switch (TokenizerState)
                {
                    case HtmlTokenizerState.Data:
                        if (ReadDataToken(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CharacterReferenceInData:
                        if (ReadCharacterReferenceInData(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.RcData:
                        if (ReadRcData(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CharacterReferenceInRcData:
                        if (ReadCharacterReferenceInRcData(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.RawText:
                        if (ReadRawText(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptData:
                        if (ReadScriptData(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.PlainText:
                        if (ReadPlainText(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.TagOpen:
                        if (ReadTagOpen(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.EndTagOpen:
                        if (ReadEndTagOpen(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.TagName:
                        if (ReadTagName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.RcDataLessThan:
                        if (ReadRcDataLessThan(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.RcDataEndTagOpen:
                        if (ReadRcDataEndTagOpen(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.RcDataEndTagName:
                        if (ReadRcDataEndTagName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.RawTextLessThan:
                        if (ReadRawTextLessThan(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.RawTextEndTagOpen:
                        if (ReadRawTextEndTagOpen(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.RawTextEndTagName:
                        if (ReadRawTextEndTagName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataLessThan:
                        if (ReadScriptDataLessThan(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEndTagOpen:
                        if (ReadScriptDataEndTagOpen(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEndTagName:
                        if (ReadScriptDataEndTagName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEscapeStart:
                        if (ReadScriptDataEscapeStart(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEscapeStartDash:
                        if (ReadScriptDataEscapeStartDash(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEscaped:
                        if (ReadScriptDataEscaped(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedDash:
                        if (ReadScriptDataEscapedDash(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedDashDash:
                        if (ReadScriptDataEscapedDashDash(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedLessThan:
                        if (ReadScriptDataEscapedLessThan(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedEndTagOpen:
                        if (ReadScriptDataEscapedEndTagOpen(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataEscapedEndTagName:
                        if (ReadScriptDataEscapedEndTagName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapeStart:
                        if (ReadScriptDataDoubleEscapeStart(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscaped:
                        if (ReadScriptDataDoubleEscaped(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapedDash:
                        if (ReadScriptDataDoubleEscapedDash(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapedDashDash:
                        if (ReadScriptDataDoubleEscapedDashDash(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapedLessThan:
                        if (ReadScriptDataDoubleEscapedLessThan(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.ScriptDataDoubleEscapeEnd:
                        if (ReadScriptDataDoubleEscapeEnd(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.BeforeAttributeName:
                        if (ReadBeforeAttributeName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AttributeName:
                        if (ReadAttributeName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AfterAttributeName:
                        if (ReadAfterAttributeName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.BeforeAttributeValue:
                        if (ReadBeforeAttributeValue(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AttributeValueQuoted:
                        if (ReadAttributeValueQuoted(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AttributeValueUnquoted:
                        if (ReadAttributeValueUnquoted(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CharacterReferenceInAttributeValue:
                        if (ReadCharacterReferenceInAttributeValue(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AfterAttributeValueQuoted:
                        if (ReadAfterAttributeValueQuoted(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.SelfClosingStartTag:
                        if (ReadSelfClosingStartTag(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.BogusComment:
                        if (ReadBogusComment(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.MarkupDeclarationOpen:
                        if (ReadMarkupDeclarationOpen(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CommentStart:
                        if (ReadCommentStart(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CommentStartDash:
                        if (ReadCommentStartDash(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.Comment:
                        if (ReadComment(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CommentEndDash:
                        if (ReadCommentEndDash(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CommentEnd:
                        if (ReadCommentEnd(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CommentEndBang:
                        if (ReadCommentEndBang(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.DocType:
                        if (ReadDocType(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.BeforeDocTypeName:
                        if (ReadBeforeDocTypeName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.DocTypeName:
                        if (ReadDocTypeName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AfterDocTypeName:
                        if (ReadAfterDocTypeName(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AfterDocTypePublicKeyword:
                        if (ReadAfterDocTypePublicKeyword(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.BeforeDocTypePublicIdentifier:
                        if (ReadBeforeDocTypePublicIdentifier(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.DocTypePublicIdentifierQuoted:
                        if (ReadDocTypePublicIdentifierQuoted(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AfterDocTypePublicIdentifier:
                        if (ReadAfterDocTypePublicIdentifier(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.BetweenDocTypePublicAndSystemIdentifiers:
                        if (ReadBetweenDocTypePublicAndSystemIdentifiers(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AfterDocTypeSystemKeyword:
                        if (ReadAfterDocTypeSystemKeyword(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.BeforeDocTypeSystemIdentifier:
                        if (ReadBeforeDocTypeSystemIdentifier(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.DocTypeSystemIdentifierQuoted:
                        if (ReadDocTypeSystemIdentifierQuoted(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.AfterDocTypeSystemIdentifier:
                        if (ReadAfterDocTypeSystemIdentifier(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.BogusDocType:
                        if (ReadBogusDocType(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.CDataSection:
                        if (ReadCDataSection(out token))
                            return true;
                        break;
                    case HtmlTokenizerState.EndOfFile:
                        token = null;
                        return false;
                }
            } while (true);
        }
    }
}
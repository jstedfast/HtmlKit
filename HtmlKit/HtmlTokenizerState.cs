//
// HtmlTokenizerState.cs
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

namespace HtmlKit {
	/// <summary>
	/// The HTML tokenizer state.
	/// </summary>
	/// <remarks>
	/// The HTML tokenizer state.
	/// </remarks>
	public enum HtmlTokenizerState {
		Data,
		CharacterReferenceInData,
		RcData, // todo
		CharacterReferenceInRcData, // todo
		RawText, // todo
		ScriptData, // todo
		PlainText, // todo
		TagOpen,
		EndTagOpen,
		TagName,
		RcDataLessThan, // todo
		RcDataEndTagOpen, // todo
		RcDataEndTagName, // todo
		RawTextLessThan, // todo
		RawTextEndTagOpen, // todo
		RawTextEndTagName, // todo
		ScriptDataLessThan, // todo
		ScriptDataEndTagOpen, // todo
		ScriptDataEndTagName, // todo
		ScriptDataEscapeStart, // todo
		ScriptDataEscapeStartDash, // todo
		ScriptDataEscaped, // todo
		ScriptDataEscapedDash, // todo
		ScriptDataEscapedDashDash, // todo
		ScriptDataEscapedLessThan, // todo
		ScriptDataEscapedEndTagOpen, // todo
		ScriptDataEscapedEndTagName, // todo
		ScriptDataDoubleEscapeStart, // todo
		ScriptDataDoubleEscaped, // todo
		ScriptDataDoubleEscapedDash, // todo
		ScriptDataDoubleEscapedDashDash, // todo
		ScriptDataDoubleEscapedLessThan, // todo
		ScriptDataDoubleEscapeEnd, // todo
		BeforeAttributeName,
		AttributeName,
		AfterAttributeName,
		BeforeAttributeValue,
		AttributeValueQuoted,
		AttributeValueUnquoted,
		CharacterReferenceInAttributeValue,
		AfterAttributeValueQuoted,
		SelfClosingStartTag,
		BogusComment,
		MarkupDeclarationOpen,
		CommentStart,
		CommentStartDash,
		Comment,
		CommentEndDash,
		CommentEnd,
		CommentEndBang,
		DocType,
		BeforeDocTypeName,
		DocTypeName,
		AfterDocTypeName,
		AfterDocTypePublicKeyword,
		BeforeDocTypePublicIdentifier,
		DocTypePublicIdentifierQuoted,
		AfterDocTypePublicIdentifier,
		BetweenDocTypePublicAndSystemIdentifiers,
		AfterDocTypeSystemKeyword,
		BeforeDocTypeSystemIdentifier,
		DocTypeSystemIdentifierQuoted,
		AfterDocTypeSystemIdentifier,
		BogusDocType,
		CDataSection, // todo
		EndOfFile
	}
}

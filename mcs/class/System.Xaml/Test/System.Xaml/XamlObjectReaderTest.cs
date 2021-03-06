//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml;
using System.Xaml.Schema;
using System.Xml;
using NUnit.Framework;

using CategoryAttribute = NUnit.Framework.CategoryAttribute;

// Some test result remarks:
// - TypeExtension: [ConstructorArgument] -> PositionalParameters
// - StaticExtension: almost identical to TypeExtension
// - Reference: [ConstructorArgument], [ContentProperty] -> only ordinal member.
// - ArrayExtension: [ConstrutorArgument], [ContentProperty] -> no PositionalParameters, Items.
// - NullExtension: no member.
// - MyExtension: [ConstructorArgument] -> only ordinal members...hmm?

namespace MonoTests.System.Xaml
{
	[TestFixture]
	public partial class XamlObjectReaderTest : XamlReaderTestBase
	{
		[Test]
		public void ConstructorNullObject ()
		{
			// allowed.
			new XamlObjectReader (null);
		}

		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void ConstructorNullSchemaContext ()
		{
			new XamlObjectReader ("foo", (XamlSchemaContext) null);
		}

		[Test]
		public void ConstructorNullSettings ()
		{
			new XamlObjectReader ("foo", (XamlObjectReaderSettings) null);
		}

		[Test]
		[ExpectedException (typeof (ArgumentNullException))]
		public void ConstructorNullSchemaContext2 ()
		{
			new XamlObjectReader ("foo", null, new XamlObjectReaderSettings ());
		}

		[Test]
		public void ConstructorNullSettings2 ()
		{
			new XamlObjectReader ("foo", new XamlSchemaContext (null, null), null);
		}

		[Test]
		[ExpectedException (typeof (XamlObjectReaderException))]
		public void ReadNonConstructible ()
		{
			// XamlType has no default constructor.
			new XamlObjectReader (XamlLanguage.String);
		}

		[Test]
		[ExpectedException (typeof (XamlObjectReaderException))]
		public void NonPublicType ()
		{
			new XamlObjectReader (new TestClass1 ());
		}

		[Test]
		[ExpectedException (typeof (XamlObjectReaderException))]
		public void NestedType ()
		{
			new XamlObjectReader (new TestClass2 ());
		}
		
		public class TestClass2
		{
		}

		[Test]
		public void ConstructibleType ()
		{
			new XamlObjectReader (new TestClass3 ());
		}
	}
	
	public partial class XamlReaderTestBase
	{
		protected void Read_String (XamlReader r)
		{
			Assert.AreEqual (XamlNodeType.None, r.NodeType, "#1");
			Assert.IsNull (r.Member, "#2");
			Assert.IsNull (r.Namespace, "#3");
			Assert.IsNull (r.Member, "#4");
			Assert.IsNull (r.Type, "#5");
			Assert.IsNull (r.Value, "#6");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			Assert.IsNotNull (r.Type, "#23");
			Assert.AreEqual (new XamlType (typeof (string), r.SchemaContext), r.Type, "#23-2");
			Assert.IsNull (r.Namespace, "#25");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.IsNotNull (r.Member, "#33");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "#33-2");
			Assert.IsNull (r.Type, "#34");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			Assert.AreEqual ("foo", r.Value, "#43");
			Assert.IsNull (r.Member, "#44");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");
			Assert.IsNull (r.Type, "#53");
			Assert.IsNull (r.Member, "#54");

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#62");
			Assert.IsNull (r.Type, "#63");

			Assert.IsFalse (r.Read (), "#71");
			Assert.IsTrue (r.IsEof, "#72");
		}

		protected void WriteNullMemberAsObject (XamlReader r, Action validateNullInstance)
		{
			Assert.AreEqual (XamlNodeType.None, r.NodeType, "#1");
			Assert.IsTrue (r.Read (), "#6");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#7");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "#7-2");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "#7-3");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#12-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#12-3");

			Assert.IsTrue (r.Read (), "#16");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#17");
			var xt = new XamlType (typeof (TestClass4), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#17-2");
//			Assert.IsTrue (r.Instance is TestClass4, "#17-3");
			Assert.AreEqual (2, xt.GetAllMembers ().Count, "#17-4");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#22");
			Assert.AreEqual (xt.GetMember ("Bar"), r.Member, "#22-2");

			Assert.IsTrue (r.Read (), "#26");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#27");
			Assert.AreEqual (XamlLanguage.Null, r.Type, "#27-2");
			if (validateNullInstance != null)
				validateNullInstance ();

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#32");

			Assert.IsTrue (r.Read (), "#36");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#37");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#42");
			Assert.AreEqual (xt.GetMember ("Foo"), r.Member, "#42-2");

			Assert.IsTrue (r.Read (), "#43");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#43-2");
			Assert.AreEqual (XamlLanguage.Null, r.Type, "#43-3");
			if (validateNullInstance != null)
				validateNullInstance ();

			Assert.IsTrue (r.Read (), "#44");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#44-2");

			Assert.IsTrue (r.Read (), "#46");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#47");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#52");

			Assert.IsFalse (r.Read (), "#56");
			Assert.IsTrue (r.IsEof, "#57");
		}
		
		protected void StaticMember (XamlReader r)
		{
			Assert.AreEqual (XamlNodeType.None, r.NodeType, "#1");
			Assert.IsTrue (r.Read (), "#6");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#7");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "#7-2");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "#7-3");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#12-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#12-3");

			Assert.IsTrue (r.Read (), "#16");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#17");
			var xt = new XamlType (typeof (TestClass5), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#17-2");
//			Assert.IsTrue (r.Instance is TestClass5, "#17-3");
			Assert.AreEqual (2, xt.GetAllMembers ().Count, "#17-4");
			Assert.IsTrue (xt.GetAllMembers ().Any (xm => xm.Name == "Bar"), "#17-5");
			Assert.IsTrue (xt.GetAllMembers ().Any (xm => xm.Name == "Baz"), "#17-6");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#22");
			Assert.AreEqual (xt.GetMember ("Bar"), r.Member, "#22-2");

			Assert.IsTrue (r.Read (), "#26");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#27");
			Assert.AreEqual (XamlLanguage.Null, r.Type, "#27-2");
//			Assert.IsNull (r.Instance, "#27-3");

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#32");

			Assert.IsTrue (r.Read (), "#36");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#37");
			// static Foo is not included in GetAllXembers() return value.
			// ReadOnly is not included in GetAllMembers() return value neither.
			// nonpublic Baz is a member, but does not appear in the reader.

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#52");

			Assert.IsFalse (r.Read (), "#56");
			Assert.IsTrue (r.IsEof, "#57");
		}

		protected void Skip (XamlReader r)
		{
			r.Skip ();
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1");
			r.Skip ();
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#2");
			r.Skip ();
			Assert.IsTrue (r.IsEof, "#3");
		}

		protected void Skip2 (XamlReader r)
		{
			r.Read (); // NamespaceDeclaration
			r.Read (); // Type
			if (r is XamlXmlReader)
				ReadBase (r);
			r.Read (); // Member (Initialization)
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#1");
			r.Skip ();
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#2");
			r.Skip ();
			Assert.IsTrue (r.IsEof, "#3");
		}

		protected void Read_XmlDocument (XamlReader r)
		{
			for (int i = 0; i < 3; i++) {
				r.Read ();
				Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1-" + i);
			}
			r.Read ();

			Assert.AreEqual (new XamlType (typeof (XmlDocument), r.SchemaContext), r.Type, "#2");
			r.Read ();
			var l = new List<XamlMember> ();
			while (r.NodeType == XamlNodeType.StartMember) {
			// It depends on XmlDocument's implenentation details. It fails on mono only because XmlDocument.SchemaInfo overrides both getter and setter.
			//for (int i = 0; i < 5; i++) {
			//	Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#3-" + i);
				l.Add (r.Member);
				r.Skip ();
			}
			Assert.IsNotNull (l.FirstOrDefault (m => m.Name == "Value"), "#4-1");
			Assert.IsNotNull (l.FirstOrDefault (m => m.Name == "InnerXml"), "#4-2");
			Assert.IsNotNull (l.FirstOrDefault (m => m.Name == "Prefix"), "#4-3");
			Assert.IsNotNull (l.FirstOrDefault (m => m.Name == "PreserveWhitespace"), "#4-4");
			Assert.IsNotNull (l.FirstOrDefault (m => m.Name == "Schemas"), "#4-5");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#5");
			Assert.IsFalse (r.Read (), "#6");
		}

		protected void Read_NonPrimitive (XamlReader r)
		{
			Assert.AreEqual (XamlNodeType.None, r.NodeType, "#1");
			Assert.IsTrue (r.Read (), "#6");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#7");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "#7-2");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "#7-3");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#12-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#12-3");

			Assert.IsTrue (r.Read (), "#16");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#17");
			var xt = new XamlType (typeof (TestClass3), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#17-2");
//			Assert.IsTrue (r.Instance is TestClass3, "#17-3");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#22");
			Assert.AreEqual (xt.GetMember ("Nested"), r.Member, "#22-2");

			Assert.IsTrue (r.Read (), "#26");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#27");
			Assert.AreEqual (XamlLanguage.Null, r.Type, "#27-2");
//			Assert.IsNull (r.Instance, "#27-3");

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#32");

			Assert.IsTrue (r.Read (), "#36");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#37");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#42");

			Assert.IsFalse (r.Read (), "#46");
			Assert.IsTrue (r.IsEof, "#47");
		}

		protected void Read_TypeOrTypeExtension (XamlReader r, Action validateInstance, XamlMember ctorArgMember)
		{
			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");
//			Assert.IsNull (r.Instance, "#14");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			Assert.IsNotNull (r.Type, "#23");
			Assert.AreEqual (XamlLanguage.Type, r.Type, "#23-2");
			Assert.IsNull (r.Namespace, "#25");
			if (validateInstance != null)
				validateInstance ();

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.IsNotNull (r.Member, "#33");
			Assert.AreEqual (ctorArgMember, r.Member, "#33-2");
			Assert.IsNull (r.Type, "#34");
//			Assert.IsNull (r.Instance, "#35");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			Assert.IsNotNull (r.Value, "#43");
			Assert.AreEqual ("x:Int32", r.Value, "#43-2");
			Assert.IsNull (r.Member, "#44");
//			Assert.IsNull (r.Instance, "#45");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");
			Assert.IsNull (r.Type, "#53");
			Assert.IsNull (r.Member, "#54");
//			Assert.IsNull (r.Instance, "#55");

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#62");
			Assert.IsNull (r.Type, "#63");

			Assert.IsFalse (r.Read (), "#71");
			Assert.IsTrue (r.IsEof, "#72");
		}

		protected void Read_TypeOrTypeExtension2 (XamlReader r, Action validateInstance, XamlMember ctorArgMember)
		{
			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");

			var defns = "clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name;

			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (defns, r.Namespace.Namespace, "#13-3:" + r.Namespace.Prefix);

			Assert.IsTrue (r.Read (), "#16");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#17");
			Assert.IsNotNull (r.Namespace, "#18");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#18-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#18-3:" + r.Namespace.Prefix);

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			Assert.AreEqual (new XamlType (typeof (TypeExtension), r.SchemaContext), r.Type, "#23-2");
			if (validateInstance != null)
				validateInstance ();

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.AreEqual (ctorArgMember, r.Member, "#33-2");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			Assert.AreEqual ("TestClass1", r.Value, "#43-2");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#62");

			Assert.IsFalse (r.Read (), "#71");
			Assert.IsTrue (r.IsEof, "#72");
		}

		protected void Read_Reference (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			var xt = new XamlType (typeof (Reference), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#23-2");
//			Assert.IsTrue (r.Instance is Reference, "#26");
			Assert.IsNotNull (XamlLanguage.Type.SchemaContext, "#23-3");
			Assert.IsNotNull (r.SchemaContext, "#23-4");
			Assert.AreNotEqual (XamlLanguage.Type.SchemaContext, r.SchemaContext, "#23-5");
			Assert.AreNotEqual (XamlLanguage.Reference.SchemaContext, xt.SchemaContext, "#23-6");
			Assert.AreEqual (XamlLanguage.Reference, xt, "#23-7");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			// unlike TypeExtension there is no PositionalParameters.
			Assert.AreEqual (xt.GetMember ("Name"), r.Member, "#33-2");

			// It is a ContentProperty (besides [ConstructorArgument])
			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			Assert.AreEqual ("FooBar", r.Value, "#43-2");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#62");

			Assert.IsFalse (r.Read (), "#71");
			Assert.IsTrue (r.IsEof, "#72");
		}

		protected void Read_NullOrNullExtension (XamlReader r, Action validateInstance)
		{
			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");
//			Assert.IsNull (r.Instance, "#14");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			Assert.AreEqual (new XamlType (typeof (NullExtension), r.SchemaContext), r.Type, "#23-2");
			if (validateInstance != null)
				validateInstance ();

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#62");

			Assert.IsFalse (r.Read (), "#71");
			Assert.IsTrue (r.IsEof, "#72");
		}

		// almost identical to TypeExtension (only type/instance difference)
		protected void Read_StaticExtension (XamlReader r, XamlMember ctorArgMember)
		{
			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");
//			Assert.IsNull (r.Instance, "#14");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			Assert.AreEqual (new XamlType (typeof (StaticExtension), r.SchemaContext), r.Type, "#23-2");
//			Assert.IsTrue (r.Instance is StaticExtension, "#26");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.AreEqual (ctorArgMember, r.Member, "#33-2");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			Assert.AreEqual ("FooBar", r.Value, "#43-2");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#62");

			Assert.IsFalse (r.Read (), "#71");
			Assert.IsTrue (r.IsEof, "#72");
		}

		protected void Read_ListInt32 (XamlReader r, Action validateInstance, List<int> obj)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");

			var defns = "clr-namespace:System.Collections.Generic;assembly=mscorlib";

			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-3");
			Assert.AreEqual (defns, r.Namespace.Namespace, "ns#1-4");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			var xt = new XamlType (typeof (List<int>), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#23");
			Assert.IsTrue (xt.IsCollection, "#27");
			if (validateInstance != null)
				validateInstance ();

			// This assumption on member ordering ("Type" then "Items") is somewhat wrong, and we might have to adjust it in the future.

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.AreEqual (xt.GetMember ("Capacity"), r.Member, "#33");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			// The value is implementation details, not testable.
			//Assert.AreEqual ("3", r.Value, "#43");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			if (obj.Count > 0) { // only when items exist.

			Assert.IsTrue (r.Read (), "#72");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#72-2");
			Assert.AreEqual (XamlLanguage.Items, r.Member, "#72-3");

			string [] values = {"5", "-3", "2147483647", "0"};
			for (int i = 0; i < 4; i++) {
				Assert.IsTrue (r.Read (), i + "#73");
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, i + "#73-2");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, i + "#74-2");
				Assert.AreEqual (XamlLanguage.Initialization, r.Member, i + "#74-3");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.IsNotNull (r.Value, i + "#75-2");
				Assert.AreEqual (values [i], r.Value, i + "#73-3");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, i + "#74-2");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, i + "#75-2");
			}

			Assert.IsTrue (r.Read (), "#81");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#82"); // XamlLanguage.Items
			
			} // end of "if count > 0".

			Assert.IsTrue (r.Read (), "#87");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#88");

			Assert.IsFalse (r.Read (), "#89");
		}

		protected void Read_ListType (XamlReader r, bool isObjectReader)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");

			var defns = "clr-namespace:System.Collections.Generic;assembly=mscorlib";
			var defns2 = "clr-namespace:System;assembly=mscorlib";
			var defns3 = "clr-namespace:System.Xaml;assembly=System.Xaml";

			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-3");
			Assert.AreEqual (defns, r.Namespace.Namespace, "ns#1-4");

			Assert.IsTrue (r.Read (), "ns#2-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2-2");
			Assert.IsNotNull (r.Namespace, "ns#2-3");
			Assert.AreEqual ("s", r.Namespace.Prefix, "ns#2-3-2");
			Assert.AreEqual (defns2, r.Namespace.Namespace, "ns#2-3-3");

			Assert.IsTrue (r.Read (), "ns#3-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#3-2");
			Assert.IsNotNull (r.Namespace, "ns#3-3");
			Assert.AreEqual ("sx", r.Namespace.Prefix, "ns#3-3-2");
			Assert.AreEqual (defns3, r.Namespace.Namespace, "ns#3-3-3");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			var xt = new XamlType (typeof (List<Type>), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#23");
			Assert.IsTrue (xt.IsCollection, "#27");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.AreEqual (xt.GetMember ("Capacity"), r.Member, "#33");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			Assert.AreEqual ("2", r.Value, "#43");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			Assert.IsTrue (r.Read (), "#72");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#72-2");
			Assert.AreEqual (XamlLanguage.Items, r.Member, "#72-3");

			string [] values = {"x:Int32", "Dictionary(s:Type, sx:XamlType)"};
			for (int i = 0; i < 2; i++) {
				Assert.IsTrue (r.Read (), i + "#73");
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, i + "#73-2");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, i + "#74-2");
				// Here XamlObjectReader and XamlXmlReader significantly differs. (Lucky we can make this test conditional so simply)
				if (isObjectReader)
					Assert.AreEqual (XamlLanguage.PositionalParameters, r.Member, i + "#74-3");
				else
					Assert.AreEqual (XamlLanguage.Type.GetMember ("Type"), r.Member, i + "#74-3");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.IsNotNull (r.Value, i + "#75-2");
				Assert.AreEqual (values [i], r.Value, i + "#73-3");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, i + "#74-2");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, i + "#75-2");
			}

			Assert.IsTrue (r.Read (), "#81");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#82"); // XamlLanguage.Items
			
			Assert.IsTrue (r.Read (), "#87");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#88");

			Assert.IsFalse (r.Read (), "#89");
		}

		protected void Read_ArrayList (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");

			var defns = "clr-namespace:System.Collections;assembly=mscorlib";

			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-3");
			Assert.AreEqual (defns, r.Namespace.Namespace, "ns#1-4");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			var xt = new XamlType (typeof (ArrayList), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#23");
//			Assert.AreEqual (obj, r.Instance, "#26");
			Assert.IsTrue (xt.IsCollection, "#27");

			if (r is XamlXmlReader)
				ReadBase (r);

			// This assumption on member ordering ("Type" then "Items") is somewhat wrong, and we might have to adjust it in the future.

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.AreEqual (xt.GetMember ("Capacity"), r.Member, "#33");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			// The value is implementation details, not testable.
			//Assert.AreEqual ("3", r.Value, "#43");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			Assert.IsTrue (r.Read (), "#72");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#72-2");
			Assert.AreEqual (XamlLanguage.Items, r.Member, "#72-3");

			string [] values = {"5", "-3", "0"};
			for (int i = 0; i < 3; i++) {
				Assert.IsTrue (r.Read (), i + "#73");
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, i + "#73-2");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, i + "#74-2");
				Assert.AreEqual (XamlLanguage.Initialization, r.Member, i + "#74-3");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.IsNotNull (r.Value, i + "#75-2");
				Assert.AreEqual (values [i], r.Value, i + "#73-3");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, i + "#74-2");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, i + "#75-2");
			}

			Assert.IsTrue (r.Read (), "#81");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#82"); // XamlLanguage.Items

			Assert.IsTrue (r.Read (), "#87");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#88");

			Assert.IsFalse (r.Read (), "#89");
		}

		protected void Read_ArrayOrArrayExtensionOrMyArrayExtension (XamlReader r, Action validateInstance, Type extType)
		{
			if (extType == typeof (MyArrayExtension)) {
				Assert.IsTrue (r.Read (), "#1");
				Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#2");
				Assert.IsNotNull (r.Namespace, "#3");
				Assert.AreEqual (String.Empty, r.Namespace.Prefix, "#3-2");
			}
			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			var xt = new XamlType (extType, r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#23");
			if (validateInstance != null)
				validateInstance ();

			if (r is XamlXmlReader)
				ReadBase (r);

			// This assumption on member ordering ("Type" then "Items") is somewhat wrong, and we might have to adjust it in the future.

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.AreEqual (xt.GetMember ("Type"), r.Member, "#33");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			Assert.AreEqual ("x:Int32", r.Value, "#43");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#62");
			Assert.AreEqual (xt.GetMember ("Items"), r.Member, "#63");

			Assert.IsTrue (r.Read (), "#71");
			Assert.AreEqual (XamlNodeType.GetObject, r.NodeType, "#71-2");
			Assert.IsNull (r.Type, "#71-3");
			Assert.IsNull (r.Member, "#71-4");
			Assert.IsNull (r.Value, "#71-5");

			Assert.IsTrue (r.Read (), "#72");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#72-2");
			Assert.AreEqual (XamlLanguage.Items, r.Member, "#72-3");

			string [] values = {"5", "-3", "0"};
			for (int i = 0; i < 3; i++) {
				Assert.IsTrue (r.Read (), i + "#73");
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, i + "#73-2");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, i + "#74-2");
				Assert.AreEqual (XamlLanguage.Initialization, r.Member, i + "#74-3");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.IsNotNull (r.Value, i + "#75-2");
				Assert.AreEqual (values [i], r.Value, i + "#73-3");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, i + "#74-2");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, i + "#75-2");
			}

			Assert.IsTrue (r.Read (), "#81");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#82"); // XamlLanguage.Items

			Assert.IsTrue (r.Read (), "#83");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#84"); // GetObject

			Assert.IsTrue (r.Read (), "#85");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#86"); // ArrayExtension.Items

			Assert.IsTrue (r.Read (), "#87");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#88"); // ArrayExtension

			Assert.IsFalse (r.Read (), "#89");
		}

		// It gives Type member, not PositionalParameters... and no Items member here.
		protected void Read_ArrayExtension2 (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");
//			Assert.IsNull (r.Instance, "#14");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			var xt = new XamlType (typeof (ArrayExtension), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#23-2");
//			Assert.IsTrue (r.Instance is ArrayExtension, "#26");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.AreEqual (xt.GetMember ("Type"), r.Member, "#33-2");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			Assert.AreEqual ("x:Int32", r.Value, "#43-2");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#62");

			Assert.IsFalse (r.Read (), "#71");
			Assert.IsTrue (r.IsEof, "#72");
		}
	}

	public partial class XamlObjectReaderTest
	{
		[Test]
		public void Read_String ()
		{
			var r = new XamlObjectReader ("foo");
			Read_String (r);
		}

		[Test]
		public void WriteNullMemberAsObject ()
		{
			var r = new XamlObjectReader (new TestClass4 ());
			WriteNullMemberAsObject (r, delegate {
				Assert.IsNull (r.Instance, "#x"); }
				);
		}
		
		[Test]
		public void StaticMember ()
		{
			var r = new XamlObjectReader (new TestClass5 ());
			StaticMember (r);
		}

		[Test]
		public void Skip ()
		{
			var r = new XamlObjectReader ("Foo");
			Skip (r);
		}
		
		[Test]
		public void Skip2 ()
		{
			var r = new XamlObjectReader ("Foo");
			Skip2 (r);
		}

		[Test]
		public void Read_XmlDocument ()
		{
			var doc = new XmlDocument ();
			doc.LoadXml ("<root xmlns='urn:foo'><elem attr='val' /></root>");
			var r = new XamlObjectReader (doc);
			Read_XmlDocument (r);
		}

		[Test]
		public void Read_NonPrimitive ()
		{
			var r = new XamlObjectReader (new TestClass3 ());
			Read_NonPrimitive (r);
		}
		
		[Test]
		public void Read_Type ()
		{
			var r = new XamlObjectReader (typeof (int));
			Read_TypeOrTypeExtension (r);
		}
		
		[Test]
		public void Read_TypeExtension ()
		{
			var tx = new TypeExtension (typeof (int));
			var r = new XamlObjectReader (tx);
			Read_TypeOrTypeExtension (r);
		}

		void Read_TypeOrTypeExtension (XamlObjectReader r)
		{
			Read_TypeOrTypeExtension (r, delegate {
				Assert.IsTrue (r.Instance is TypeExtension, "#26");
				}, XamlLanguage.PositionalParameters);
		}
		
		[Test]
		public void Read_Type2 ()
		{
			var r = new XamlObjectReader (typeof (TestClass1));
			Read_TypeOrTypeExtension2 (r);
		}
		
		[Test]
		public void Read_TypeExtension2 ()
		{
			var r = new XamlObjectReader (new TypeExtension (typeof (TestClass1)));
			Read_TypeOrTypeExtension2 (r);
		}

		void Read_TypeOrTypeExtension2 (XamlObjectReader r)
		{
			Read_TypeOrTypeExtension2 (r, delegate {
				Assert.IsTrue (r.Instance is TypeExtension, "#26");
			}, XamlLanguage.PositionalParameters);
		}
		
		[Test]
		public void Read_Reference ()
		{
			var r = new XamlObjectReader (new Reference ("FooBar"));
			Read_Reference (r);
		}
		
		[Test]
		public void Read_Null ()
		{
			var r = new XamlObjectReader (null);
			Read_NullOrNullExtension (r, (object) null);
		}

		[Test]
		public void Read_NullExtension ()
		{
			var o = new NullExtension ();
			var r = new XamlObjectReader (o);
			Read_NullOrNullExtension (r, o);
		}
		
		void Read_NullOrNullExtension (XamlObjectReader r, object instance)
		{
			Read_NullOrNullExtension (r, delegate {
				Assert.AreEqual (instance, r.Instance, "#26"); // null and NullExtension are different here.
			});
		}
		
		[Test]
		public void Read_StaticExtension ()
		{
			var r = new XamlObjectReader (new StaticExtension ("FooBar"));
			Read_StaticExtension (r, XamlLanguage.PositionalParameters);
		}
		
		[Test]
		public void Read_ListInt32 ()
		{
			var obj = new List<int> (new int [] {5, -3, int.MaxValue, 0});
			Read_ListInt32 (obj);
		}
		
		[Test]
		public void Read_ListInt32_2 ()
		{
			var obj = new List<int> (new int [0]);
			Read_ListInt32 (obj);
		}
		
		void Read_ListInt32 (List<int> obj)
		{
			var r = new XamlObjectReader (obj);
			Read_ListInt32 (r, delegate {
				Assert.AreEqual (obj, r.Instance, "#26");
				}, obj);
		}
		
		[Test]
		public void Read_ListType ()
		{
			var obj = new List<Type> (new Type [] {typeof (int), typeof (Dictionary<Type, XamlType>)}) { Capacity = 2 };
			var r = new XamlObjectReader (obj);
			Read_ListType (r, true);
		}

		[Test]
		public void Read_ArrayList ()
		{
			var obj = new ArrayList (new int [] {5, -3, 0});
			var r = new XamlObjectReader (obj);
			Read_ArrayList (r);
		}
		
		[Test]
		public void Read_Array ()
		{
			var obj = new int [] {5, -3, 0};
			var r = new XamlObjectReader (obj);
			Read_ArrayOrArrayExtension (r, obj);
		}
		
		[Test]
		public void Read_ArrayExtension ()
		{
			var obj = new ArrayExtension (new int [] {5, -3, 0});
			var r = new XamlObjectReader (obj);
			Read_ArrayOrArrayExtension (r, obj);
		}
		
		[Test]
		public void Read_MyArrayExtension ()
		{
			var obj = new MyArrayExtension (new int [] {5, -3, 0});
			var r = new XamlObjectReader (obj);
			Read_ArrayOrArrayExtensionOrMyArrayExtension (r, obj, typeof (MyArrayExtension));
		}

		void Read_ArrayOrArrayExtension (XamlObjectReader r, object instance)
		{
			Read_ArrayOrArrayExtensionOrMyArrayExtension (r, instance, typeof (ArrayExtension));
		}

		void Read_ArrayOrArrayExtensionOrMyArrayExtension (XamlObjectReader r, object instance, Type extType)
		{
			Read_ArrayOrArrayExtensionOrMyArrayExtension (r, delegate {
				Assert.AreEqual (instance, r.Instance, "#26"); // different between Array and ArrayExtension. Also, different from Type and TypeExtension (Type returns TypeExtension, while Array remains to return Array)
				}, extType);
		}

		[Test]
		public void Read_ArrayExtension2 ()
		{
			var r = new XamlObjectReader (new ArrayExtension (typeof (int)));
			Read_ArrayExtension2 (r);
		}
		
		[Test]
		public void Read_DateTime ()
		{
			var obj = new DateTime (2010, 4, 15);
			var r = new XamlObjectReader (obj);
			Read_CommonClrType (r, obj);
			Assert.AreEqual ("2010-04-15", Read_Initialization (r, null), "#1");
		}

		[Test]
		public void Read_TimeSpan ()
		{
			Read_CommonXamlPrimitive (TimeSpan.FromMinutes (4));
		}

		[Test]
		public void Read_Uri ()
		{
			Read_CommonXamlPrimitive (new Uri ("urn:foo"));
		}

		[Test]
		public void Read_Guid ()
		{
			var obj = Guid.NewGuid ();
			var r = new XamlObjectReader (obj);
			Assert.IsNotNull (r.SchemaContext.GetXamlType (typeof (Guid)).TypeConverter, "premise#1");
			Read_CommonClrType (r, obj);
			Assert.AreEqual (obj.ToString (), Read_Initialization (r, null), "#1");
		}

		[Test]
		[ExpectedException (typeof (XamlObjectReaderException))]
		[Category ("NotWorking")]
		public void Read_XData ()
		{
			var r = new XamlObjectReader (new XData () {Text = "xdata text"}); // XmlReader implementation is not visible.
			while (!r.IsEof)
				r.Read ();
		}

		[Test]
		public void ReadStandardTypes ()
		{
			SimpleReadStandardType (new ArrayExtension ());
			SimpleReadStandardType (new NullExtension ());
			SimpleReadStandardType (new PropertyDefinition ());
			SimpleReadStandardType (new Reference ());
			SimpleReadStandardType (new StaticExtension ());
			SimpleReadStandardType (new TypeExtension ());
		}

		void SimpleReadStandardType (object instance)
		{
			var r = new XamlObjectReader (instance);
			while (!r.IsEof)
				r.Read ();
		}

		[Test]
		public void Read_CustomMarkupExtension ()
		{
			var r = new XamlObjectReader (new MyExtension () { Foo = typeof (int), Bar = "v2", Baz = "v7"});
			Read_CustomMarkupExtension (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension2 ()
		{
			var r = new XamlObjectReader (new MyExtension2 () { Foo = typeof (int), Bar = "v2"});
			Read_CustomMarkupExtension2 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension3 ()
		{
			var r = new XamlObjectReader (new MyExtension3 () { Foo = typeof (int), Bar = "v2"});
			Read_CustomMarkupExtension3 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension4 ()
		{
			var r = new XamlObjectReader (new MyExtension4 () { Foo = typeof (int), Bar = "v2"});
			Read_CustomMarkupExtension4 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension5 ()
		{
			// This cannot be written to XamlXmlWriter though...

			var r = new XamlObjectReader (new MyExtension5 ("foo", "bar"));
			Read_CustomMarkupExtension5 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension6 ()
		{
			var r = new XamlObjectReader (new MyExtension6 ("foo"));
			Read_CustomMarkupExtension6 (r);
		}

		[Test]
		public void Read_ArgumentAttributed ()
		{
			var obj = new ArgumentAttributed ("foo", "bar");
			var r = new XamlObjectReader (obj);
			Read_ArgumentAttributed (r, obj);
		}
	}

	public partial class XamlReaderTestBase
	{
		protected void Read_CustomMarkupExtension (XamlReader r)
		{
			r.Read (); // ns
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1");
			r.Read (); // ns
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1-2");
			r.Read ();
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#2-0");
			Assert.IsFalse (r.IsEof, "#1");
			var xt = r.Type;

			if (r is XamlXmlReader)
				ReadBase (r);

			r.Read ();
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#2-1");
			Assert.IsFalse (r.IsEof, "#2-2");
			Assert.AreEqual (xt.GetMember ("Bar"), r.Member, "#2-3");

			Assert.IsTrue (r.Read (), "#2-4");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#2-5");
			Assert.AreEqual ("v2", r.Value, "#2-6");

			Assert.IsTrue (r.Read (), "#2-7");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#2-8");

			r.Read ();
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#3-1");
			Assert.IsFalse (r.IsEof, "#3-2");
			Assert.AreEqual (xt.GetMember ("Baz"), r.Member, "#3-3");

			Assert.IsTrue (r.Read (), "#3-4");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#3-5");
			Assert.AreEqual ("v7", r.Value, "#3-6");

			Assert.IsTrue (r.Read (), "#3-7");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#3-8");
			
			r.Read ();
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#4-1");
			Assert.IsFalse (r.IsEof, "#4-2");
			Assert.AreEqual (xt.GetMember ("Foo"), r.Member, "#4-3");
			Assert.IsTrue (r.Read (), "#4-4");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#4-5");
			Assert.AreEqual ("x:Int32", r.Value, "#4-6");

			Assert.IsTrue (r.Read (), "#4-7");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#4-8");

			Assert.IsTrue (r.Read (), "#5");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#5-2");

			Assert.IsFalse (r.Read (), "#6");
		}

		protected void Read_CustomMarkupExtension2 (XamlReader r)
		{
			r.Read (); // ns
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1");
			r.Read (); // note that there wasn't another NamespaceDeclaration.
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#2-0");
			var xt = r.Type;
			Assert.AreEqual (r.SchemaContext.GetXamlType (typeof (MyExtension2)), xt, "#2");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#3");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#3-2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "#4");
			Assert.IsTrue (r.Read (), "#5");
			Assert.AreEqual ("MonoTests.System.Xaml.MyExtension2", r.Value, "#6");
			Assert.IsTrue (r.Read (), "#7"); // EndMember
			Assert.IsTrue (r.Read (), "#8"); // EndObject
			Assert.IsFalse (r.Read (), "#9");
		}

		protected void Read_CustomMarkupExtension3 (XamlReader r)
		{
			r.Read (); // ns
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1");
			r.Read (); // note that there wasn't another NamespaceDeclaration.
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#2-0");
			var xt = r.Type;
			Assert.AreEqual (r.SchemaContext.GetXamlType (typeof (MyExtension3)), xt, "#2");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#3");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#3-2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "#4");
			Assert.IsTrue (r.Read (), "#5");
			Assert.AreEqual ("MonoTests.System.Xaml.MyExtension3", r.Value, "#6");
			Assert.IsTrue (r.Read (), "#7"); // EndMember
			Assert.IsTrue (r.Read (), "#8"); // EndObject
			Assert.IsFalse (r.Read (), "#9");
		}

		protected void Read_CustomMarkupExtension4 (XamlReader r)
		{
			r.Read (); // ns
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1");
			r.Read (); // note that there wasn't another NamespaceDeclaration.
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#2-0");
			var xt = r.Type;
			Assert.AreEqual (r.SchemaContext.GetXamlType (typeof (MyExtension4)), xt, "#2");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#3");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#3-2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "#4");
			Assert.IsTrue (r.Read (), "#5");
			Assert.AreEqual ("MonoTests.System.Xaml.MyExtension4", r.Value, "#6");
			Assert.IsTrue (r.Read (), "#7"); // EndMember
			Assert.IsTrue (r.Read (), "#8"); // EndObject
			Assert.IsFalse (r.Read (), "#9");
		}

		protected void Read_CustomMarkupExtension5 (XamlReader r)
		{
			r.Read (); // ns
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1");
			r.Read (); // note that there wasn't another NamespaceDeclaration.
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#2-0");
			var xt = r.Type;
			Assert.AreEqual (r.SchemaContext.GetXamlType (typeof (MyExtension5)), xt, "#2");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#3");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#3-2");
			Assert.AreEqual (XamlLanguage.PositionalParameters, r.Member, "#4");
			Assert.IsTrue (r.Read (), "#5");
			Assert.AreEqual ("foo", r.Value, "#6");
			Assert.IsTrue (r.Read (), "#7");
			Assert.AreEqual ("bar", r.Value, "#8");
			Assert.IsTrue (r.Read (), "#9");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#10");
			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#12");
			Assert.IsFalse (r.Read (), "#13");
		}

		protected void Read_CustomMarkupExtension6 (XamlReader r)
		{
			r.Read (); // ns
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#1");
			r.Read (); // note that there wasn't another NamespaceDeclaration.
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#2-0");
			var xt = r.Type;
			Assert.AreEqual (r.SchemaContext.GetXamlType (typeof (MyExtension6)), xt, "#2");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#3");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#3-2");
			Assert.AreEqual (xt.GetMember ("Foo"), r.Member, "#4"); // this is the difference between MyExtension5 and MyExtension6: it outputs constructor arguments as normal members
			Assert.IsTrue (r.Read (), "#5");
			Assert.AreEqual ("foo", r.Value, "#6");
			Assert.IsTrue (r.Read (), "#7"); // EndMember
			Assert.IsTrue (r.Read (), "#8"); // EndObject
			Assert.IsFalse (r.Read (), "#9");
		}

		protected void Read_ArgumentAttributed (XamlReader r, object obj)
		{
			Read_CommonClrType (r, obj, new KeyValuePair<string,string> ("x", XamlLanguage.Xaml2006Namespace));

			if (r is XamlXmlReader)
				ReadBase (r);

			var args = Read_AttributedArguments_String (r, new string [] {"arg1", "arg2"});
			Assert.AreEqual ("foo", args [0], "#1");
			Assert.AreEqual ("bar", args [1], "#2");
		}

		protected void Read_Dictionary (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual ("clr-namespace:System.Collections.Generic;assembly=mscorlib", r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "ns#2-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2-2");
			Assert.IsNotNull (r.Namespace, "ns#2-3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ns#2-4");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns#2-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (Dictionary<string,object>), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");
//			Assert.AreEqual (obj, r.Instance, "so#1-4");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "smitems#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smitems#2");
			Assert.AreEqual (XamlLanguage.Items, r.Member, "smitems#3");

			for (int i = 0; i < 2; i++) {

				// start of an item
				Assert.IsTrue (r.Read (), "soi#1-1." + i);
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "soi#1-2." + i);
				var xt2 = new XamlType (typeof (double), r.SchemaContext);
				Assert.AreEqual (xt2, r.Type, "soi#1-3." + i);

				Assert.IsTrue (r.Read (), "smi#1-1." + i);
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smi#1-2." + i);
				Assert.AreEqual (XamlLanguage.Key, r.Member, "smi#1-3." + i);

				Assert.IsTrue (r.Read (), "svi#1-1." + i);
				Assert.AreEqual (XamlNodeType.Value, r.NodeType, "svi#1-2." + i);
				Assert.AreEqual (i == 0 ? "Foo" : "Bar", r.Value, "svi#1-3." + i);

				Assert.IsTrue (r.Read (), "emi#1-1." + i);
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emi#1-2." + i);

				Assert.IsTrue (r.Read (), "smi#2-1." + i);
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smi#2-2." + i);
				Assert.AreEqual (XamlLanguage.Initialization, r.Member, "smi#2-3." + i);

				Assert.IsTrue (r.Read (), "svi#2-1." + i);
				Assert.AreEqual (XamlNodeType.Value, r.NodeType, "svi#2-2." + i);
				Assert.AreEqual (i == 0 ? "5" : "-6.5", r.Value, "svi#2-3." + i); // converted to string(!)

				Assert.IsTrue (r.Read (), "emi#2-1." + i);
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emi#2-2." + i);

				Assert.IsTrue (r.Read (), "eoi#1-1." + i);
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eoi#1-2." + i);
				// end of an item
			}

			Assert.IsTrue (r.Read (), "emitems#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emitems#2"); // XamlLanguage.Items

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2"); // Dictionary

			Assert.IsFalse (r.Read (), "end");
		}

		protected void Read_Dictionary2 (XamlReader r, XamlMember ctorArgMember)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual ("clr-namespace:System.Collections.Generic;assembly=mscorlib", r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "ns#2-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2-2");
			Assert.IsNotNull (r.Namespace, "ns#2-3");
			Assert.AreEqual ("s", r.Namespace.Prefix, "ns#2-4");
			Assert.AreEqual ("clr-namespace:System;assembly=mscorlib", r.Namespace.Namespace, "ns#2-5");

			Assert.IsTrue (r.Read (), "ns#3-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#3-2");
			Assert.IsNotNull (r.Namespace, "ns#3-3");
			Assert.AreEqual ("sx", r.Namespace.Prefix, "ns#3-4");
			Assert.AreEqual ("clr-namespace:System.Xaml;assembly=System.Xaml", r.Namespace.Namespace, "ns#3-5");

			Assert.IsTrue (r.Read (), "ns#4-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#4-2");
			Assert.IsNotNull (r.Namespace, "ns#4-3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ns#4-4");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns#4-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (Dictionary<string,Type>), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");
//			Assert.AreEqual (obj, r.Instance, "so#1-4");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "smitems#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smitems#2");
			Assert.AreEqual (XamlLanguage.Items, r.Member, "smitems#3");

			for (int i = 0; i < 2; i++) {

				// start of an item
				Assert.IsTrue (r.Read (), "soi#1-1." + i);
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "soi#1-2." + i);
				var xt2 = XamlLanguage.Type;
				Assert.AreEqual (xt2, r.Type, "soi#1-3." + i);

				if (r is XamlObjectReader) {
					Read_Dictionary2_ConstructorArgument (r, ctorArgMember, i);
					Read_Dictionary2_Key (r, i);
				} else {
					Read_Dictionary2_Key (r, i);
					Read_Dictionary2_ConstructorArgument (r, ctorArgMember, i);
				}

				Assert.IsTrue (r.Read (), "eoi#1-1." + i);
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eoi#1-2." + i);
				// end of an item
			}

			Assert.IsTrue (r.Read (), "emitems#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emitems#2"); // XamlLanguage.Items

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2"); // Dictionary

			Assert.IsFalse (r.Read (), "end");
		}
		
		void Read_Dictionary2_ConstructorArgument (XamlReader r, XamlMember ctorArgMember, int i)
		{
			Assert.IsTrue (r.Read (), "smi#1-1." + i);
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smi#1-2." + i);
			Assert.AreEqual (ctorArgMember, r.Member, "smi#1-3." + i);

			Assert.IsTrue (r.Read (), "svi#1-1." + i);
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "svi#1-2." + i);
			Assert.AreEqual (i == 0 ? "x:Int32" : "Dictionary(s:Type, sx:XamlType)", r.Value, "svi#1-3." + i);

			Assert.IsTrue (r.Read (), "emi#1-1." + i);
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emi#1-2." + i);
		}

		void Read_Dictionary2_Key (XamlReader r, int i)
		{
			Assert.IsTrue (r.Read (), "smi#2-1." + i);
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smi#2-2." + i);
			Assert.AreEqual (XamlLanguage.Key, r.Member, "smi#2-3." + i);

			Assert.IsTrue (r.Read (), "svi#2-1." + i);
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "svi#2-2." + i);
			Assert.AreEqual (i == 0 ? "Foo" : "Bar", r.Value, "svi#2-3." + i);

			Assert.IsTrue (r.Read (), "emi#2-1." + i);
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emi#2-2." + i);
		}

		protected void PositionalParameters1 (XamlReader r)
		{
			// ns1 > T:PositionalParametersClass1 > M:_PositionalParameters > foo > 5 > EM:_PositionalParameters > ET:PositionalParametersClass1

			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (PositionalParametersClass1), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");
//			Assert.AreEqual (obj, r.Instance, "so#1-4");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "sposprm#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sposprm#2");
			Assert.AreEqual (XamlLanguage.PositionalParameters, r.Member, "sposprm#3");

			Assert.IsTrue (r.Read (), "sva#1-1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "sva#1-2");
			Assert.AreEqual ("foo", r.Value, "sva#1-3");

			Assert.IsTrue (r.Read (), "sva#2-1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "sva#2-2");
			Assert.AreEqual ("5", r.Value, "sva#2-3");

			Assert.IsTrue (r.Read (), "eposprm#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "eposprm#2"); // XamlLanguage.PositionalParameters

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2");

			Assert.IsFalse (r.Read (), "end");
		}
		
		protected void PositionalParameters2 (XamlReader r)
		{
			// ns1 > T:PositionalParametersWrapper > M:Body > T:PositionalParametersClass1 > M:_PositionalParameters > foo > 5 > EM:_PositionalParameters > ET:PositionalParametersClass1

			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (PositionalParametersWrapper), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");
//			Assert.AreEqual (obj, r.Instance, "so#1-4");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "sm#1-1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sm#1-2");
			Assert.AreEqual (xt.GetMember ("Body"), r.Member, "sm#1-3");

			xt = new XamlType (typeof (PositionalParametersClass1), r.SchemaContext);
			Assert.IsTrue (r.Read (), "so#2-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2-2");
			Assert.AreEqual (xt, r.Type, "so#2-3");
//			Assert.AreEqual (obj.Body, r.Instance, "so#2-4");

			Assert.IsTrue (r.Read (), "sposprm#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sposprm#2");
			Assert.AreEqual (XamlLanguage.PositionalParameters, r.Member, "sposprm#3");

			Assert.IsTrue (r.Read (), "sva#1-1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "sva#1-2");
			Assert.AreEqual ("foo", r.Value, "sva#1-3");

			Assert.IsTrue (r.Read (), "sva#2-1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "sva#2-2");
			Assert.AreEqual ("5", r.Value, "sva#2-3");

			Assert.IsTrue (r.Read (), "eposprm#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "eposprm#2"); // XamlLanguage.PositionalParameters

			Assert.IsTrue (r.Read (), "eo#2-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2-2");

			Assert.IsTrue (r.Read (), "em#1-1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "eo#1-2");

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2");

			Assert.IsFalse (r.Read (), "end");
		}
		
		protected void ComplexPositionalParameters (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "ns#2-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2-2");
			Assert.IsNotNull (r.Namespace, "ns#2-3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ns#2-4");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns#2-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (ComplexPositionalParameterWrapper), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");
//			Assert.AreEqual (obj, r.Instance, "so#1-4");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "sm#1-1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sm#1-2");
			Assert.AreEqual (xt.GetMember ("Param"), r.Member, "sm#1-3");

			xt = r.SchemaContext.GetXamlType (typeof (ComplexPositionalParameterClass));
			Assert.IsTrue (r.Read (), "so#2-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2-2");
			Assert.AreEqual (xt, r.Type, "so#2-3");
//			Assert.AreEqual (obj.Param, r.Instance, "so#2-4");

			Assert.IsTrue (r.Read (), "sarg#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sarg#2");
			Assert.AreEqual (XamlLanguage.Arguments, r.Member, "sarg#3");

			Assert.IsTrue (r.Read (), "so#3-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#3-2");
			xt = r.SchemaContext.GetXamlType (typeof (ComplexPositionalParameterValue));
			Assert.AreEqual (xt, r.Type, "so#3-3");

			Assert.IsTrue (r.Read (), "sm#3-1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sm#3-2");
			Assert.AreEqual (xt.GetMember ("Foo"), r.Member, "sm#3-3");
			Assert.IsTrue (r.Read (), "v#3-1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "v#3-2");
			Assert.AreEqual ("foo", r.Value, "v#3-3");

			Assert.IsTrue (r.Read (), "em#3-1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "em#3-2");
			Assert.IsTrue (r.Read (), "eo#3-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#3-2");

			Assert.IsTrue (r.Read (), "earg#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "earg#2"); // XamlLanguage.Arguments

			Assert.IsTrue (r.Read (), "eo#2-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2-2");

			Assert.IsTrue (r.Read (), "em#1-1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "eo#1-2");

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2");

			Assert.IsFalse (r.Read (), "end");
		}

		protected void Read_ListWrapper (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#2");
			Assert.IsNotNull (r.Namespace, "#3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "#3-2");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			var xt = new XamlType (typeof (ListWrapper), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#23");
//			Assert.AreEqual (obj, r.Instance, "#26");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#62");
			Assert.AreEqual (xt.GetMember ("Items"), r.Member, "#63");

			Assert.IsTrue (r.Read (), "#71");
			Assert.AreEqual (XamlNodeType.GetObject, r.NodeType, "#71-2");
			Assert.IsNull (r.Type, "#71-3");
			Assert.IsNull (r.Member, "#71-4");
			Assert.IsNull (r.Value, "#71-5");

			Assert.IsTrue (r.Read (), "#72");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#72-2");
			Assert.AreEqual (XamlLanguage.Items, r.Member, "#72-3");

			string [] values = {"5", "-3", "0"};
			for (int i = 0; i < 3; i++) {
				Assert.IsTrue (r.Read (), i + "#73");
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, i + "#73-2");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, i + "#74-2");
				Assert.AreEqual (XamlLanguage.Initialization, r.Member, i + "#74-3");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.IsNotNull (r.Value, i + "#75-2");
				Assert.AreEqual (values [i], r.Value, i + "#73-3");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, i + "#74-2");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, i + "#75-2");
			}

			Assert.IsTrue (r.Read (), "#81");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#82"); // XamlLanguage.Items

			Assert.IsTrue (r.Read (), "#83");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#84"); // GetObject

			Assert.IsTrue (r.Read (), "#85");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#86"); // ListWrapper.Items

			Assert.IsTrue (r.Read (), "#87");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#88"); // ListWrapper

			Assert.IsFalse (r.Read (), "#89");
		}

		protected void Read_ListWrapper2 (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#2");
			Assert.IsNotNull (r.Namespace, "#3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "#3-2");

			Assert.IsTrue (r.Read (), "#6");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#7");
			Assert.IsNotNull (r.Namespace, "#8");
			Assert.AreEqual ("scg", r.Namespace.Prefix, "#8-2");
			Assert.AreEqual ("clr-namespace:System.Collections.Generic;assembly=mscorlib", r.Namespace.Namespace, "#8-3");

			Assert.IsTrue (r.Read (), "#11");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "#12");
			Assert.IsNotNull (r.Namespace, "#13");
			Assert.AreEqual ("x", r.Namespace.Prefix, "#13-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "#13-3");

			Assert.IsTrue (r.Read (), "#21");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#22");
			var xt = new XamlType (typeof (ListWrapper2), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "#23");
//			Assert.AreEqual (obj, r.Instance, "#26");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "#61");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#62");
			Assert.AreEqual (xt.GetMember ("Items"), r.Member, "#63");

			Assert.IsTrue (r.Read (), "#71");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "#71-2");
			xt = r.SchemaContext.GetXamlType (typeof (List<int>));
			Assert.AreEqual (xt, r.Type, "#71-3");
			Assert.IsNull (r.Member, "#71-4");
			Assert.IsNull (r.Value, "#71-5");

			// Capacity
			Assert.IsTrue (r.Read (), "#31");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#32");
			Assert.AreEqual (xt.GetMember ("Capacity"), r.Member, "#33");

			Assert.IsTrue (r.Read (), "#41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#42");
			// The value is implementation details, not testable.
			//Assert.AreEqual ("3", r.Value, "#43");

			Assert.IsTrue (r.Read (), "#51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#52");

			// Items
			Assert.IsTrue (r.Read (), "#72");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "#72-2");
			Assert.AreEqual (XamlLanguage.Items, r.Member, "#72-3");

			string [] values = {"5", "-3", "0"};
			for (int i = 0; i < 3; i++) {
				Assert.IsTrue (r.Read (), i + "#73");
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, i + "#73-2");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, i + "#74-2");
				Assert.AreEqual (XamlLanguage.Initialization, r.Member, i + "#74-3");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.IsNotNull (r.Value, i + "#75-2");
				Assert.AreEqual (values [i], r.Value, i + "#73-3");
				Assert.IsTrue (r.Read (), i + "#74");
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, i + "#74-2");
				Assert.IsTrue (r.Read (), i + "#75");
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, i + "#75-2");
			}

			Assert.IsTrue (r.Read (), "#81");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#82"); // XamlLanguage.Items

			Assert.IsTrue (r.Read (), "#83");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#84"); // StartObject(of List<int>)

			Assert.IsTrue (r.Read (), "#85");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#86"); // ListWrapper.Items

			Assert.IsTrue (r.Read (), "#87");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "#88"); // ListWrapper

			Assert.IsFalse (r.Read (), "#89");
		}
		
		protected void Read_ContentIncluded (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (ContentIncludedClass), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "sposprm#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sposprm#2");
			Assert.AreEqual (xt.GetMember ("Content"), r.Member, "sposprm#3");

			Assert.IsTrue (r.Read (), "sva#1-1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "sva#1-2");
			Assert.AreEqual ("foo", r.Value, "sva#1-3");

			Assert.IsTrue (r.Read (), "eposprm#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "eposprm#2");

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2");

			Assert.IsFalse (r.Read (), "end");
		}
		
		protected void Read_PropertyDefinition (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (PropertyDefinition), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "smod#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smod#2");
			Assert.AreEqual (xt.GetMember ("Modifier"), r.Member, "smod#3");

			Assert.IsTrue (r.Read (), "vmod#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vmod#2");
			Assert.AreEqual ("protected", r.Value, "vmod#3");

			Assert.IsTrue (r.Read (), "emod#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emod#2");

			Assert.IsTrue (r.Read (), "sname#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sname#2");
			Assert.AreEqual (xt.GetMember ("Name"), r.Member, "sname#3");

			Assert.IsTrue (r.Read (), "vname#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vname#2");
			Assert.AreEqual ("foo", r.Value, "vname#3");

			Assert.IsTrue (r.Read (), "ename#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "ename#2");

			Assert.IsTrue (r.Read (), "stype#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "stype#2");
			Assert.AreEqual (xt.GetMember ("Type"), r.Member, "stype#3");

			Assert.IsTrue (r.Read (), "vtype#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vtype#2");
			Assert.AreEqual ("x:String", r.Value, "vtype#3");

			Assert.IsTrue (r.Read (), "etype#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "etype#2");

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2");

			Assert.IsFalse (r.Read (), "end");
		}

		protected void Read_StaticExtensionWrapper (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual ("", r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "ns#2-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2-2");
			Assert.IsNotNull (r.Namespace, "ns#2-3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ns#2-4");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns#2-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (StaticExtensionWrapper), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "sprm#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sprm#2");
			Assert.AreEqual (xt.GetMember ("Param"), r.Member, "sprm#3");

			Assert.IsTrue (r.Read (), "so#2-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2-2");
			xt = new XamlType (typeof (StaticExtension), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#2-3");

			Assert.IsTrue (r.Read (), "smbr#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smbr#2");
			Assert.AreEqual (XamlLanguage.PositionalParameters, r.Member, "smbr#3");

			Assert.IsTrue (r.Read (), "vmbr#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vmbr#2");
			Assert.AreEqual ("StaticExtensionWrapper.Foo", r.Value, "vmbr#3");

			Assert.IsTrue (r.Read (), "embr#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "embr#2");

			Assert.IsTrue (r.Read (), "eo#2-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2-2");

			Assert.IsTrue (r.Read (), "emod#1-1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emod#1-2");

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2");

			Assert.IsFalse (r.Read (), "end");
		}

		protected void Read_TypeExtensionWrapper (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "ns#1-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#1-2");
			Assert.IsNotNull (r.Namespace, "ns#1-3");
			Assert.AreEqual ("", r.Namespace.Prefix, "ns#1-4");
			Assert.AreEqual ("clr-namespace:MonoTests.System.Xaml;assembly=" + GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "ns#1-5");

			Assert.IsTrue (r.Read (), "ns#2-1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2-2");
			Assert.IsNotNull (r.Namespace, "ns#2-3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ns#2-4");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns#2-5");

			Assert.IsTrue (r.Read (), "so#1-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#1-2");
			var xt = new XamlType (typeof (TypeExtensionWrapper), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#1-3");

			if (r is XamlXmlReader)
				ReadBase (r);

			Assert.IsTrue (r.Read (), "sprm#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sprm#2");
			Assert.AreEqual (xt.GetMember ("Param"), r.Member, "sprm#3");

			Assert.IsTrue (r.Read (), "so#2-1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2-2");
			xt = new XamlType (typeof (TypeExtension), r.SchemaContext);
			Assert.AreEqual (xt, r.Type, "so#2-3");

			Assert.IsTrue (r.Read (), "smbr#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "smbr#2");
			Assert.AreEqual (XamlLanguage.PositionalParameters, r.Member, "smbr#3");

			Assert.IsTrue (r.Read (), "vmbr#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vmbr#2");
			Assert.AreEqual (String.Empty, r.Value, "vmbr#3");

			Assert.IsTrue (r.Read (), "embr#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "embr#2");

			Assert.IsTrue (r.Read (), "eo#2-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2-2");

			Assert.IsTrue (r.Read (), "emod#1-1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "emod#1-2");

			Assert.IsTrue (r.Read (), "eo#1-1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#1-2");

			Assert.IsFalse (r.Read (), "end");
		}

		protected void Read_CommonXamlPrimitive (object obj)
		{
			var r = new XamlObjectReader (obj);
			Read_CommonXamlType (r);
			Read_Initialization (r, obj);
		}

		// from StartMember of Initialization to EndMember
		protected string Read_Initialization (XamlReader r, object comparableValue)
		{
			Assert.IsTrue (r.Read (), "init#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "init#2");
			Assert.IsNotNull (r.Member, "init#3");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "init#3-2");
			Assert.IsTrue (r.Read (), "init#4");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "init#5");
			Assert.AreEqual (typeof (string), r.Value.GetType (), "init#6");
			string ret = (string) r.Value;
			if (comparableValue != null)
				Assert.AreEqual (comparableValue.ToString (), r.Value, "init#6-2");
			Assert.IsTrue (r.Read (), "init#7");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "init#8");
			return ret;
		}

		protected object [] Read_AttributedArguments_String (XamlReader r, string [] argNames) // valid only for string arguments.
		{
			object [] ret = new object [argNames.Length];

			Assert.IsTrue (r.Read (), "attarg.Arguments.Start1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "attarg.Arguments.Start2");
			Assert.IsNotNull (r.Member, "attarg.Arguments.Start3");
			Assert.AreEqual (XamlLanguage.Arguments, r.Member, "attarg.Arguments.Start4");
			for (int i = 0; i < argNames.Length; i++) {
				string arg = argNames [i];
				Assert.IsTrue (r.Read (), "attarg.ArgStartObject1." + arg);
				Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "attarg.ArgStartObject2." + arg);
				Assert.AreEqual (typeof (string), r.Type.UnderlyingType, "attarg.ArgStartObject3." + arg);
				Assert.IsTrue (r.Read (), "attarg.ArgStartMember1." + arg);
				Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "attarg.ArgStartMember2." + arg);
				Assert.AreEqual (XamlLanguage.Initialization, r.Member, "attarg.ArgStartMember3." + arg); // (as the argument is string here by definition)
				Assert.IsTrue (r.Read (), "attarg.ArgValue1." + arg);
				Assert.AreEqual (XamlNodeType.Value, r.NodeType, "attarg.ArgValue2." + arg);
				Assert.AreEqual (typeof (string), r.Value.GetType (), "attarg.ArgValue3." + arg);
				ret [i] = r.Value;
				Assert.IsTrue (r.Read (), "attarg.ArgEndMember1." + arg);
				Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "attarg.ArgEndMember2." + arg);
				Assert.IsTrue (r.Read (), "attarg.ArgEndObject1." + arg);
				Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "attarg.ArgEndObject2." + arg);
			}
			Assert.IsTrue (r.Read (), "attarg.Arguments.End1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "attarg.Arguments.End2");
			return ret;
		}

		// from initial to StartObject
		protected void Read_CommonXamlType (XamlObjectReader r)
		{
			Read_CommonXamlType (r, delegate {
				Assert.IsNull (r.Instance, "ct#4");
				});
		}
		
		protected void Read_CommonXamlType (XamlReader r, Action validateInstance)
		{
			Assert.IsTrue (r.Read (), "ct#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ct#2");
			Assert.IsNotNull (r.Namespace, "ct#3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ct#3-2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ct#3-3");
			if (validateInstance != null)
				validateInstance ();

			Assert.IsTrue (r.Read (), "ct#5");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "ct#6");
		}

		// from initial to StartObject
		protected void Read_CommonClrType (XamlReader r, object obj, params KeyValuePair<string,string> [] additionalNamespaces)
		{
			Assert.IsTrue (r.Read (), "ct#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ct#2");
			Assert.IsNotNull (r.Namespace, "ct#3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ct#3-2");
			Assert.AreEqual ("clr-namespace:" + obj.GetType ().Namespace + ";assembly=" + obj.GetType ().Assembly.GetName ().Name, r.Namespace.Namespace, "ct#3-3");

			foreach (var kvp in additionalNamespaces) {
				Assert.IsTrue (r.Read (), "ct#4." + kvp.Key);
				Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ct#5." + kvp.Key);
				Assert.IsNotNull (r.Namespace, "ct#6." + kvp.Key);
				Assert.AreEqual (kvp.Key, r.Namespace.Prefix, "ct#6-2." + kvp.Key);
				Assert.AreEqual (kvp.Value, r.Namespace.Namespace, "ct#6-3." + kvp.Key);
			}

			Assert.IsTrue (r.Read (), "ct#7");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "ct#8");
		}

		protected void ReadBase (XamlReader r)
		{
			Assert.IsTrue (r.Read (), "sbase#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sbase#2");
			Assert.AreEqual (XamlLanguage.Base, r.Member, "sbase#3");

			Assert.IsTrue (r.Read (), "vbase#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vbase#2");
			Assert.IsTrue (r.Value is string, "vbase#3");

			Assert.IsTrue (r.Read (), "ebase#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "ebase#2");
		}
	}

	public partial class XamlObjectReaderTest
	{
		[Test]
		public void Read_Dictionary ()
		{
			var obj = new Dictionary<string,object> ();
			obj ["Foo"] = 5.0;
			obj ["Bar"] = -6.5;
			var r = new XamlObjectReader (obj);
			Read_Dictionary (r);
		}
		
		[Test]
		public void Read_Dictionary2 ()
		{
			var obj = new Dictionary<string,Type> ();
			obj ["Foo"] = typeof (int);
			obj ["Bar"] = typeof (Dictionary<Type,XamlType>);
			var r = new XamlObjectReader (obj);
			Read_Dictionary2 (r, XamlLanguage.PositionalParameters);
		}
		
		[Test]
		public void PositionalParameters1 ()
		{
			// Note: this can be read, but cannot be written to XML.
			var obj = new PositionalParametersClass1 ("foo", 5);
			var r = new XamlObjectReader (obj);
			PositionalParameters1 (r);
		}
		
		[Test]
		public void PositionalParameters2 ()
		{
			var obj = new PositionalParametersWrapper ("foo", 5);
			var r = new XamlObjectReader (obj);
			PositionalParameters2 (r);
		}

		[Test]
		public void ComplexPositionalParameters ()
		{
			var obj = new ComplexPositionalParameterWrapper () { Param = new ComplexPositionalParameterClass (new ComplexPositionalParameterValue () { Foo = "foo" })};
			var r = new XamlObjectReader (obj);
			ComplexPositionalParameters (r);
		}
		
		[Test]
		public void Read_ListWrapper ()
		{
			var obj = new ListWrapper (new List<int> (new int [] {5, -3, 0}));
			var r = new XamlObjectReader (obj);
			Read_ListWrapper (r);
		}
		
		[Test]
		public void Read_ListWrapper2 () // read-write list member.
		{
			var obj = new ListWrapper2 (new List<int> (new int [] {5, -3, 0}));
			var r = new XamlObjectReader (obj);
			Read_ListWrapper2 (r);
		}

		[Test]
		public void Read_ContentIncluded ()
		{
			var obj = new ContentIncludedClass () { Content = "foo" };
			var r = new XamlObjectReader (obj);
			Read_ContentIncluded (r);
		}

		[Test]
		public void Read_PropertyDefinition ()
		{
			var obj = new PropertyDefinition () { Modifier = "protected", Name = "foo", Type = XamlLanguage.String };
			var r = new XamlObjectReader (obj);
			Read_PropertyDefinition (r);
		}

		[Test]
		[Category ("NotWorking")]
		public void Read_StaticExtensionWrapper ()
		{
			var obj = new StaticExtensionWrapper () { Param = new StaticExtension ("StaticExtensionWrapper.Foo") };
			var r = new XamlObjectReader (obj);
			Read_StaticExtensionWrapper (r);
		}

		[Test]
		[Category ("NotWorking")]
		public void Read_TypeExtensionWrapper ()
		{
			var obj = new TypeExtensionWrapper () { Param = new TypeExtension ("Foo") };
			var r = new XamlObjectReader (obj);
			Read_TypeExtensionWrapper (r);
		}
	}
}

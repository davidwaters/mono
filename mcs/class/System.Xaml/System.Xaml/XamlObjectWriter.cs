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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml.Schema;

namespace System.Xaml
{
	public class XamlObjectWriter : XamlWriter, IXamlLineInfoConsumer
	{
		public XamlObjectWriter (XamlSchemaContext schemaContext)
			: this (schemaContext, null)
		{
		}

		public XamlObjectWriter (XamlSchemaContext schemaContext, XamlObjectWriterSettings settings)
		{
			if (schemaContext == null)
				throw new ArgumentNullException ("schemaContext");
			this.sctx = schemaContext;
			this.settings = settings ?? new XamlObjectWriterSettings ();

			var p = new PrefixLookup (sctx);
			service_provider = new ValueSerializerContext (p, sctx);
			namespaces = p.Namespaces;
		}

		XamlSchemaContext sctx;
		XamlObjectWriterSettings settings;

		XamlWriterStateManager manager = new XamlWriterStateManager<XamlObjectWriterException, XamlObjectWriterException> (false);
		object result;
		int line = -1, column = -1;
		Stack<XamlMember> members = new Stack<XamlMember> ();

		List<NamespaceDeclaration> namespaces;
		IValueSerializerContext service_provider;
		Stack<ObjectState> object_states = new Stack<ObjectState> ();

		class ObjectState
		{
			public XamlType Type;
			public object Value;
			public object KeyValue;
			public List<object> Contents = new List<object> ();
			public List<XamlMember> WrittenProperties = new List<XamlMember> ();
			public bool IsInstantiated;
			public bool IsGetObject;

			public string FactoryMethod;
			public List<object> Arguments = new List<object> ();
		}

		public virtual object Result {
			get { return result; }
		}

		public INameScope RootNameScope {
			get { throw new NotImplementedException (); }
		}

		public override XamlSchemaContext SchemaContext {
			get { return sctx; }
		}

		public bool ShouldProvideLineInfo {
			get { return line > 0 && column > 0; }
		}
		
		public void Clear ()
		{
			throw new NotImplementedException ();
		}

		protected override void Dispose (bool disposing)
		{
			if (!disposing)
				return;

			while (object_states.Count > 0) {
				if (object_states.Count == members.Count)
					WriteEndMember ();
				WriteEndObject ();
			}
		}

		protected virtual void OnAfterBeginInit (object value)
		{
			throw new NotImplementedException ();
		}

		protected virtual void OnAfterEndInit (object value)
		{
			throw new NotImplementedException ();
		}

		protected virtual void OnAfterProperties (object value)
		{
			throw new NotImplementedException ();
		}

		protected virtual void OnBeforeProperties (object value)
		{
			throw new NotImplementedException ();
		}

		protected virtual bool OnSetValue (object eventSender, XamlMember member, object value)
		{
			if (settings.XamlSetValueHandler != null) {
				settings.XamlSetValueHandler (eventSender, new XamlSetValueEventArgs (member, value));
				return true;
			}
			return false;
		}

		void SetValue (XamlMember member, object value)
		{
			if (member.IsDirective)
				return;
			if (!OnSetValue (this, member, value))
				member.Invoker.SetValue (object_states.Peek ().Value, value);
		}

		public void SetLineInfo (int lineNumber, int linePosition)
		{
			line = lineNumber;
			column = linePosition;
		}

		static readonly BindingFlags static_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		[MonoTODO ("Dictionary needs implementation")]
		public override void WriteEndMember ()
		{
			manager.EndMember ();
			
			var xm = members.Pop ();
			var state = object_states.Peek ();
			var contents = state.Contents;

			if (xm == XamlLanguage.PositionalParameters) {
				manager.AcceptMultipleValues = false;
				//state.PositionalParameterIndex = -1;
				FillConstructedObject (true);
			} else if (xm == XamlLanguage.FactoryMethod) {
				if (contents.Count != 1 || !(contents [0] is string))
					throw new XamlObjectWriterException (String.Format ("FactoryMethod must be non-empty string name. {0} value exists.", contents.Count > 0 ? contents [0] : "0"));
				state.FactoryMethod = (string) contents [0];
			} else if (xm == XamlLanguage.Arguments) {
				if (state.FactoryMethod != null) {
					var mi = state.Type.UnderlyingType.GetMethods (static_flags).FirstOrDefault (mii => mii.Name == state.FactoryMethod && mii.GetParameters ().Length == contents.Count);
					if (mi == null)
						throw new XamlObjectWriterException (String.Format ("Specified static factory method '{0}' for type '{1}' was not found", state.FactoryMethod, state.Type));
					state.Value = mi.Invoke (null, contents.ToArray ());
				}
				else
					FillConstructedObject (false);
			} else if (xm == XamlLanguage.Initialization) {
				// ... and no need to do anything. The object value to pop *is* the return value.
			} else if (xm == XamlLanguage.Items) {
				var coll = state.Value;
				foreach (var content in contents)
					xm.Type.Invoker.AddToCollection (coll, content);
			} else if (xm.Type.IsDictionary) {
				throw new NotImplementedException ();
			} else {
				if (contents.Count > 1)
					throw new XamlDuplicateMemberException (String.Format ("Property '{0}' is already set to this '{1}' object", xm, state.Type));
				if (contents.Count == 1) {
					var value = contents [0];
					if (!xm.Type.IsCollection || !xm.IsReadOnly) // exclude read-only object.
						SetValue (xm, value);
				}
			}

			contents.Clear ();

			if (object_states.Count > 0)
				object_states.Peek ().WrittenProperties.Add (xm);
			//written_properties_stack.Peek ().Add (xm);
		}

		void FillConstructedObject (bool considerPositionalParameters)
		{
			var state = object_states.Peek ();

			var args = state.Type.GetSortedConstructorArguments ();
			var argt = args != null ? (IList<XamlType>) (from arg in args select arg.Type).ToArray () : considerPositionalParameters ? state.Type.GetPositionalParameters (state.Contents.Count) : null;

			var argv = new object [argt.Count];
			for (int i = 0; i < argv.Length; i++)
				argv [i] = GetCorrectlyTypedValue (argt [i], state.Contents [i]);
			state.Value = state.Type.Invoker.CreateInstance (argv);
			state.IsInstantiated = true;
		}

		object GetCorrectlyTypedValue (XamlType xt, object value)
		{
			try {
				return DoGetCorrectlyTypedValue (xt, value);
			} catch (Exception ex) {
				// For + ex.Message, the runtime should print InnerException message like .NET does.
				throw new XamlObjectWriterException (String.Format ("Could not convert object \'{0}' (of type {1}) to {2}: ", value, value != null ? (object) value.GetType () : "(null)", xt)  + ex.Message, ex);
			}
		}

		// It expects that it is not invoked when there is no value to 
		// assign.
		// When it is passed null, then it returns a default instance.
		// For example, passing null as Int32 results in 0.
		object DoGetCorrectlyTypedValue (XamlType xt, object value)
		{
			if (value == null)
				return xt.Invoker.CreateInstance (new object [0]);

			// FIXME: this could be generalized by some means, but I cannot find any.
			if (xt.UnderlyingType == typeof (Type))
				xt = XamlLanguage.Type;
			if (xt == XamlLanguage.Type && value is string)
				value = new TypeExtension ((string) value);

			// FIXME: this could be generalized by some means, but I cannot find any.
			if (xt.UnderlyingType == typeof (XamlType) && value is string) {
				var nsr = (IXamlNamespaceResolver) service_provider.GetService (typeof (IXamlNamespaceResolver));
				value = sctx.GetXamlType (XamlTypeName.Parse ((string) value, nsr));
			}
			
			if (value is MarkupExtension)
				value = ((MarkupExtension) value).ProvideValue (service_provider);

			if (IsAllowedType (xt, value))
				return value;

			if (xt.TypeConverter != null && value != null) {
				var tc = xt.TypeConverter.ConverterInstance;
				if (tc != null && tc.CanConvertFrom (value.GetType ()))
					value = tc.ConvertFrom (value);
				return value;
			}

			throw new XamlObjectWriterException (String.Format ("Value '{1}' (of type {2}) is not of or convertible to type {0}", xt, value, value != null ? (object) value.GetType () : "(null)"));
		}

		bool IsAllowedType (XamlType xt, object value)
		{
			return  xt == null ||
				xt.UnderlyingType == null ||
				xt.UnderlyingType.IsInstanceOfType (value) ||
				value == null && xt == XamlLanguage.Null ||
				xt.IsMarkupExtension && IsAllowedType (xt.MarkupExtensionReturnType, value);
		}

		public override void WriteEndObject ()
		{
			manager.EndObject (object_states.Count > 0);

			InitializeObjectIfRequired (false); // this is required for such case that there was no StartMember call.

			var state = object_states.Pop ();
			var obj = GetCorrectlyTypedValue (state.Type, state.Value);
			if (members.Count > 0) {
				var pstate = object_states.Peek ();
				pstate.Contents.Add (obj);
				pstate.WrittenProperties.Add (members.Peek ());
			}
			if (object_states.Count == 0)
				result = obj;
		}

		public override void WriteGetObject ()
		{
			manager.GetObject ();

			var xm = members.Peek ();
			// see GetObjectOnNonNullString() test. Below is invalid.
			//if (!xm.Type.IsCollection)
			//	throw new XamlObjectWriterException (String.Format ("WriteGetObject method can be invoked only when current member '{0}' is of collection type", xm.Name));

			var instance = xm.Invoker.GetValue (object_states.Peek ().Value);
			if (instance == null)
				throw new XamlObjectWriterException (String.Format ("The value  for '{0}' property is null", xm.Name));

			var state = new ObjectState () {Type = SchemaContext.GetXamlType (instance.GetType ()), Value = instance, IsInstantiated = true, IsGetObject = true};
			object_states.Push (state);
		}

		public override void WriteNamespace (NamespaceDeclaration namespaceDeclaration)
		{
			if (namespaceDeclaration == null)
				throw new ArgumentNullException ("namespaceDeclaration");

			manager.Namespace ();

			namespaces.Add (namespaceDeclaration);
		}

		public override void WriteStartMember (XamlMember property)
		{
			if (property == null)
				throw new ArgumentNullException ("property");

			manager.StartMember ();
			if (property == XamlLanguage.PositionalParameters)
				manager.AcceptMultipleValues = true;

			// FIXME: this condition needs to be examined. What is known to be prevented are: PositionalParameters, Initialization and Base (the last one sort of indicates there's a lot more).
			if (!(property is XamlDirective))
				InitializeObjectIfRequired (false);

			//var wpl = object_states.Peek ().WrittenProperties;
			// FIXME: enable this. Duplicate property check should
			// be differentiate from duplicate contents (both result
			// in XamlDuplicateMemberException though).
			// Now it is done at WriteStartObject/WriteValue, but
			// it is simply wrong.
//			if (wpl.Contains (property))
//				throw new XamlDuplicateMemberException (String.Format ("Property '{0}' is already set to this '{1}' object", property, object_states.Peek ().Type));
//			wpl.Add (property);

			members.Push (property);
		}

		void InitializeObjectIfRequired (bool waitForParameters)
		{
			var state = object_states.Peek ();
			if (state.IsInstantiated)
				return;

			if (waitForParameters && (state.Type.ConstructionRequiresArguments || state.Type.HasPositionalParameters (service_provider)))
				return;

			// FIXME: "The default techniques in absence of a factory method are to attempt to find a default constructor, then attempt to find an identified type converter on type, member, or destination type."
			// http://msdn.microsoft.com/en-us/library/system.xaml.xamllanguage.factorymethod%28VS.100%29.aspx
			object obj;
			if (state.FactoryMethod != null) // FIXME: it must be implemented and verified with tests.
				throw new NotImplementedException ();
			else
				obj = state.Type.Invoker.CreateInstance (null);
			state.Value = obj;
			state.IsInstantiated = true;
		}

		public override void WriteStartObject (XamlType xamlType)
		{
			if (xamlType == null)
				throw new ArgumentNullException ("xamlType");

			manager.StartObject ();

			var xm = members.Count > 0 ? members.Peek () : null;
			var pstate = xm != null ? object_states.Peek () : null;
			var wpl = xm == null || xm.Type.IsCollection || xm.Type.IsDictionary ? null : pstate.WrittenProperties;
			if (wpl != null && wpl.Contains (xm))
				throw new XamlDuplicateMemberException (String.Format ("Property '{0}' is already set to this '{1}' object", xm, pstate.Type));

			var cstate = new ObjectState () {Type = xamlType, IsInstantiated = false};
			object_states.Push (cstate);

			if (!xamlType.IsContentValue (service_provider))
				InitializeObjectIfRequired (true);
			
			if (wpl != null) // note that this adds to the *owner* object's properties.
				wpl.Add (xm);
		}

		public override void WriteValue (object value)
		{
			manager.Value ();

			var xm = members.Peek ();
			var state = object_states.Peek ();

			var wpl = xm != null && xm != XamlLanguage.Items ? state.WrittenProperties : null;
			if (wpl != null && wpl.Contains (xm))
				throw new XamlDuplicateMemberException (String.Format ("Property '{0}' is already set to this '{1}' object", xm, state.Type));

			if (xm == XamlLanguage.Initialization) {
				value = GetCorrectlyTypedValue (state.Type, value);
				state.Value = value;
				state.IsInstantiated = true;
			}
			else if (xm.Type.IsDictionary) {
				if (xm == XamlLanguage.Key)
					state.KeyValue = GetCorrectlyTypedValue (xm.Type.KeyType, value);
				else
					state.Contents.Add (GetCorrectlyTypedValue (xm.Type.ItemType, value));
			}
			else if (xm.Type.IsCollection)
				state.Contents.Add (GetCorrectlyTypedValue (xm.Type.ItemType, value));
			else
				state.Contents.Add (GetCorrectlyTypedValue (xm.Type, value));
			if (wpl != null)
				wpl.Add (xm);
		}
	}
}

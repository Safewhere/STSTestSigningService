using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Schema;

namespace Kombit.Samples.STSTestSigningService.Code
{
    internal static class XmlUtil
    {
        public const string XmlNs = "http://www.w3.org/XML/1998/namespace";
        public const string XmlNsNs = "http://www.w3.org/2000/xmlns/";

        public const string LanguagePrefix = "xml";
        public const string LanguageLocalname = "lang";
        public const string LanguageAttribute = LanguagePrefix + ":" + LanguageLocalname;

        public static bool IsWhitespace(char ch)
        {
            return (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n');
        }

        public static string TrimEnd(string s)
        {
            int i;
            for (i = s.Length; i > 0 && IsWhitespace(s[i - 1]); i--) ;

            if (i != s.Length)
            {
                return s.Substring(0, i);
            }

            return s;
        }

        public static string TrimStart(string s)
        {
            int i;
            for (i = 0; i < s.Length && IsWhitespace(s[i]); i++) ;

            if (i != 0)
            {
                return s.Substring(i);
            }

            return s;
        }

        public static string Trim(string s)
        {
            int i;
            for (i = 0; i < s.Length && IsWhitespace(s[i]); i++) ;
            if (i >= s.Length)
            {
                return string.Empty;
            }

            int j;
            for (j = s.Length; j > 0 && IsWhitespace(s[j - 1]); j--) ;
           
            if (i != 0 || j != s.Length)
            {
                return s.Substring(i, j - i);
            }
            return s;
        }

        // Everything below is from WIF
        public static XmlQualifiedName GetXsiType(XmlReader reader)
        {
            string xsiType = reader.GetAttribute("type", XmlSchema.InstanceNamespace);
            reader.MoveToElement();

            if (string.IsNullOrEmpty(xsiType))
            {
                return null;
            }

            return ResolveQName(reader, xsiType);
        }

        public static bool EqualsQName(XmlQualifiedName qname, string localName, string namespaceUri)
        {
            return null != qname
                && StringComparer.Ordinal.Equals(localName, qname.Name)
                && StringComparer.Ordinal.Equals(namespaceUri, qname.Namespace);
        }

        public static bool IsNil(XmlReader reader)
        {
            string xsiNil = reader.GetAttribute("nil", XmlSchema.InstanceNamespace);
            return !string.IsNullOrEmpty(xsiNil) && XmlConvert.ToBoolean(xsiNil);
        }

        public static string NormalizeEmptyString(string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }

        public static XmlQualifiedName ResolveQName(XmlReader reader, string qstring)
        {
            string name = qstring;
            string prefix = String.Empty;
            string ns = null;

            int colon = qstring.IndexOf(':'); // index of char is always ordinal
            if (colon > -1)
            {
                prefix = qstring.Substring(0, colon);
                name = qstring.Substring(colon + 1, qstring.Length - (colon + 1));
            }

            ns = reader.LookupNamespace(prefix);

            return new XmlQualifiedName(name, ns);
        }

        public static void ValidateXsiType(XmlReader reader, string expectedTypeName, string expectedTypeNamespace)
        {
            ValidateXsiType(reader, expectedTypeName, expectedTypeNamespace, false);
        }

        public static void ValidateXsiType(XmlReader reader, string expectedTypeName, string expectedTypeNamespace, bool requireDeclaration)
        {
            XmlQualifiedName declaredType = GetXsiType(reader);

            if (null == declaredType)
            {
                if (requireDeclaration)
                {
                    throw new Exception("ID4104:"+ reader.LocalName+ reader.NamespaceURI);
                }
            }
            else if (!(StringComparer.Ordinal.Equals(expectedTypeNamespace, declaredType.Namespace)
                && StringComparer.Ordinal.Equals(expectedTypeName, declaredType.Name)))
            {
                throw new Exception("ID4102: " + expectedTypeName + expectedTypeNamespace + declaredType.Name + declaredType.Namespace);
            }
        }
    }
}
using System;
using System.Xml;

namespace win2tiz
{
	class CXmlUtils
	{
		public static XmlNode findMacroChildNode(XmlNode parentNode, string name)
		{
			foreach (XmlNode childNode in parentNode.ChildNodes)
			{
				if (childNode.Name == KXml.s_kMacroTag)
				{
					XmlAttribute att = childNode.Attributes[KXml.s_kNameAttr];
					if (att != null)
					{
						if (att.Value == name)
						{
							return childNode;
						}
					}
				}
			}
			return null;
		}

		public static string getXmlValue(XmlNode nodeMacro, bool isReleaseMode)
		{
			string res = "";
			XmlAttribute attr;

			attr = nodeMacro.Attributes[KXml.s_kValueAttr];
			if (attr != null) res += attr.Value + " ";

			attr = nodeMacro.Attributes[KXml.s_kCommonValueAttr];
			if (attr != null) res += attr.Value + " ";

			if (isReleaseMode)
			{
				attr = nodeMacro.Attributes[KXml.s_kReleaseValueAttr];
				if (attr != null) res += attr.Value + " ";
			}
			else
			{
				attr = nodeMacro.Attributes[KXml.s_kDebugValueAttr];
				if (attr != null) res += attr.Value + " ";
			}

			return res.Trim();
		}
	}
}

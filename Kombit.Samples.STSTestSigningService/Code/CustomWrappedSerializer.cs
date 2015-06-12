using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Runtime;
using System.Web;
using System.Xml;

namespace Kombit.Samples.STSTestSigningService.Code
{
    public class CustomWrappedSerializer : SecurityTokenSerializer
    {
        private Saml2Assertion assertion;
        private CustomSaml2SecurityTokenHandler parent;

        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public CustomWrappedSerializer(CustomSaml2SecurityTokenHandler parent, Saml2Assertion assertion)
        {
            this.assertion = assertion;
            this.parent = parent;
        }

        protected override bool CanReadKeyIdentifierClauseCore(XmlReader reader)
        {
            return false;
        }

        protected override bool CanReadKeyIdentifierCore(XmlReader reader)
        {
            return true;
        }

        protected override bool CanReadTokenCore(XmlReader reader)
        {
            return false;
        }

        protected override bool CanWriteKeyIdentifierClauseCore(SecurityKeyIdentifierClause keyIdentifierClause)
        {
            return false;
        }

        protected override bool CanWriteKeyIdentifierCore(SecurityKeyIdentifier keyIdentifier)
        {
            return false;
        }

        protected override bool CanWriteTokenCore(SecurityToken token)
        {
            return false;
        }

        protected override SecurityKeyIdentifierClause ReadKeyIdentifierClauseCore(XmlReader reader)
        {
            throw new NotSupportedException();
        }

        protected override SecurityToken ReadTokenCore(XmlReader reader, SecurityTokenResolver tokenResolver)
        {
            throw new NotSupportedException();
        }

        protected override void WriteKeyIdentifierClauseCore(XmlWriter writer, SecurityKeyIdentifierClause keyIdentifierClause)
        {
            throw new NotSupportedException();
        }

        protected override SecurityKeyIdentifier ReadKeyIdentifierCore(XmlReader reader)
        {
            return this.parent.CustomReadSigningKeyInfo(reader, this.assertion);
        }

        protected override void WriteKeyIdentifierCore(XmlWriter writer, SecurityKeyIdentifier keyIdentifier)
        {
            this.parent.CustomWriteSigningKeyInfo(writer, keyIdentifier);
        }

        protected override void WriteTokenCore(XmlWriter writer, SecurityToken token)
        {
            throw new NotSupportedException();
        }
    }
}
using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.ComponentModel.Design;
using Dataiku.DSS.Activities.Design.Designers;
using Dataiku.DSS.Activities.Design.Properties;

namespace Dataiku.DSS.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            var builder = new AttributeTableBuilder();
            builder.ValidateTable();

            var categoryAttribute = new CategoryAttribute($"{Resources.Category}");

            builder.AddCustomAttributes(typeof(QueryDSSAPINode), categoryAttribute);
            builder.AddCustomAttributes(typeof(QueryDSSAPINode), new DesignerAttribute(typeof(QueryDSSAPINodeDesigner)));
            builder.AddCustomAttributes(typeof(QueryDSSAPINode), new HelpKeywordAttribute(""));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }
}

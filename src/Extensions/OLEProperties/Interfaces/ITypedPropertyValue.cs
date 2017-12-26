namespace OpenMcdf.Extensions.OLEProperties.Interfaces
{
    public interface ITypedPropertyValue : IBinarySerializable
    {
        bool IsArray { get; set; }

        bool IsVector { get; set; }

        object PropertyValue { get; set; }

        VTPropertyType VTType
        {
            get;
            //set;
        }
    }
}
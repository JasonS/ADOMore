ADOMore
=======

Light Weight Property Reflector for ADO Actions

Supports refelction for all sensible, primitive and value types (nullables too) from SQL data store into POCO objects.  

**Does not support:** 

- UInt16
- UInt32
- UInt64
- Byte

I think that's it. Basically this is just meant to help assuage the pain of casting IDataRecord values
int object properties constantly, and is designed for common data types that I often need to send to 
and retrieve from some data store.

No dependencies, just lock and load.



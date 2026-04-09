// ilasm /dll OtherAccessors.il

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly OtherAccessors
{
}

.class interface public abstract auto ansi import IContainerObject
{
  .custom instance void [mscorlib]System.Runtime.InteropServices.GuidAttribute::.ctor(string) = ( 01 00 24 31 39 46 41 44 43 33 33 2D 32 34 42 33
                                                                                                  2D 34 35 39 46 2D 38 41 42 41 2D 42 41 32 39 39
                                                                                                  45 38 38 45 41 33 30 00 00 )

  .method public hidebysig newslot specialname abstract virtual instance string marshal(bstr) get_Value() runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 03 68 00 00 )
  }

  .method public hidebysig newslot specialname abstract virtual instance void  let_Value([in] string  marshal(bstr) A_1) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 03 68 00 00 )
  }

  .method public hidebysig newslot specialname abstract virtual instance void  set_Value([in] class IContainerObject  marshal(interface) A_1) runtime managed internalcall
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 03 68 00 00 )
  }

  .property string Value()
  {
    .custom instance void [mscorlib]System.Runtime.InteropServices.DispIdAttribute::.ctor(int32) = ( 01 00 00 00 03 68 00 00 )
    .get instance string IContainerObject::get_Value()
    .set instance void IContainerObject::set_Value(class IContainerObject)
    .other instance void IContainerObject::let_Value(string)
  }
}

.class interface public abstract auto ansi IEventSource
{
  .method public hidebysig newslot specialname abstract virtual instance void add_Notification(class [mscorlib]System.EventHandler 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void remove_Notification(class [mscorlib]System.EventHandler 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void suspend_Notification() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void resume_Notification() cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void other_Notification() cil managed
  {
  }

  .event [mscorlib]System.EventHandler Notification
  {
    .addon instance void IEventSource::add_Notification(class [mscorlib]System.EventHandler)
    .removeon instance void IEventSource::remove_Notification(class [mscorlib]System.EventHandler)
    .other instance void IEventSource::other_Notification()
    .other instance void IEventSource::suspend_Notification()
    .other instance void IEventSource::resume_Notification()
  }
}

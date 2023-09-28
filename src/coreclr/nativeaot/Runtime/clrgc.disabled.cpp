typedef long HRESULT;

HRESULT InitializeStandaloneGC();

HRESULT InitializeGCSelector()
{
    return InitializeStandaloneGC();
}
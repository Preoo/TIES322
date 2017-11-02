namespace TIES322_udp_app
{
    internal interface IRdtProtocol
    {
        
        void RdtSend(string message);
        event HandleDatagramDelegate OnReceive;
        event HandleDatagramDelegate OnSend;
        event DeliverData OnDeliver;
    }
}
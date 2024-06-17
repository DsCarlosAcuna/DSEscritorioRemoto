using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace EscritorioRemotoDirectX.Services
{
    public static class DirectXService
    {

        private static SharpDX.Direct3D11.Device _device;
        private static OutputDuplication _outputDuplication;

        public static void InitializeDirectX()
        {
            if (_device == null)
            {
                var factory = new Factory1();
                var adapter = factory.GetAdapter1(0);
                var output = adapter.GetOutput(0);
                var output1 = output.QueryInterface<Output1>();

                _device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
                _outputDuplication = output1.DuplicateOutput(_device);

                output1.Dispose();
                output.Dispose();
                adapter.Dispose();
                factory.Dispose();
            }
        }

        public static SharpDX.Direct3D11.Device Device => _device;
        public static OutputDuplication OutputDuplication => _outputDuplication;
    }

}

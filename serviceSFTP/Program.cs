using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Renci.SshNet;
using Renci.SshNet.Messages;

namespace serviceSFTP
{
    public class SftpConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string directoryPath { get; set; }
        public string filePaths { get; set; }
        public string NamedestinationDirectory { get; set; }
        public string remoteFilePath { get; set; }
        public string privateKeyFilePath { get; set; }
        public string logFilePath { get; set; }
        public string ambiente { get; set; }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });

            if (isService)
            {
                // Ejecutar la aplicación como un servicio de Windows
                using (var host = builder.Build())
                {
                    ServiceBase.Run(new ServiceBase[] { new WindowsService(host) });
                }
            }
            else
            {
                // Ejecutar la aplicación de consola para fines de desarrollo o prueba
                using (var host = builder.Build())
                {
                    host.Run();
                }
            }
        }
    }

    public class WindowsService : ServiceBase
    {
        private readonly IHost _host;

        public WindowsService(IHost host)
        {
            _host = host;
        }

        protected override void OnStart(string[] args)
        {
            _host.Start();
        }

        protected override void OnStop()
        {
            _host.StopAsync().GetAwaiter().GetResult();
        }
    }

    public class Worker : IHostedService
    {
        private Timer _timer;
        private readonly object lockObject = new object();
        private bool isRunning = false;
        private string txtlog;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(60)); // Aquí puedes ajustar el intervalo de tiempo
            return Task.CompletedTask;
        }

        private void InitializeLogFile(string logFilePath)
        {
            // Crear el archivo de registro si no existe
            if (!File.Exists(logFilePath))
            {
                using (StreamWriter writer = File.CreateText(logFilePath))
                {
                    writer.WriteLine($"> Inicio del registro: {DateTime.Now}");
                }
            }
            else
            {
                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    writer.WriteLine($"> Inicio del registro: {DateTime.Now}");
                }
            }
        }

        private void DoWork(object state)
        {
            string log_service_all = @"C:\SFTPservice\log\error_service_SFTP.txt";

            if (!isRunning)
            {
                lock (lockObject)
                {
                    if (isRunning)
                    {
                        // Otra instancia ya está en progreso, no hagas nada
                        return;
                    }

                    isRunning = true;
                }

                Console.WriteLine("Servicio en funcionamiento...");

                try
                {
                    string rutaArchivoXml = @"C:\SFTPservice\root.xml"; // Reemplaza esto con la ruta real de tu archivo XML

                    XDocument xdoc = XDocument.Load(rutaArchivoXml); // Cargamos el archivo XML usando XDocument

                    // Creamos una instancia de la clase SftpConfig para almacenar los valores del XML
                    SftpConfig config = new SftpConfig();

                    // Accedemos a los elementos del XML y almacenamos los valores en las variables
                    config.Host = xdoc.Root.Element("host")?.Value;
                    int.TryParse(xdoc.Root.Element("port")?.Value, out int port);
                    config.Port = port;
                    config.Username = xdoc.Root.Element("username")?.Value;
                    config.Password = xdoc.Root.Element("password")?.Value;
                    config.directoryPath = xdoc.Root.Element("directoryPath")?.Value;
                    config.filePaths = xdoc.Root.Element("filePaths")?.Value;
                    config.NamedestinationDirectory = xdoc.Root.Element("NamedestinationDirectory")?.Value;
                    config.remoteFilePath = xdoc.Root.Element("remoteFilePath")?.Value;
                    config.privateKeyFilePath = xdoc.Root.Element("privateKeyFilePath")?.Value;
                    config.logFilePath = xdoc.Root.Element("logFilePath")?.Value;
                    config.ambiente = xdoc.Root.Element("ambiente")?.Value;

                    DateTime currentDate = DateTime.Today;
                    txtlog = Convert.ToString(currentDate.ToString("yyyy-MM-dd"));
                    string archivolog = string.Concat(config.logFilePath, txtlog, ".txt");

                    InitializeLogFile(archivolog);

                    if (!string.IsNullOrEmpty(config.ambiente))
                    {
                        // Verificamos si se obtuvieron los valores correctamente
                        if (config.Host != null && config.Port != 0 && config.Username != null && (config.Password == null || config.privateKeyFilePath != null))
                        {
                            // Información de conexión al servidor SFTP
                            //string host = "1.0.0.0"; // host
                            //int port = 22; // Puerto SFTP predeterminado
                            //string username = "admin"; // usuario
                            //string password = "clave"; // password

                            // logica para saber de donde se subiran los archivos
                            string new_directorio = Path.Combine(config.directoryPath, config.ambiente);

                            //string directoryPath = "C:\\upload\\"; // Directorio que deseas cargar
                            string[] filePaths = Directory.GetFiles(new_directorio, config.filePaths); // Cargar todos los archivos del directorio
                            string destinationDirectory = Path.Combine(new_directorio, config.NamedestinationDirectory); // Nombre del directorio de destino despues de subir el archivo

                            if (filePaths.Length == 0)
                            {
                                Console.WriteLine($"No hay archivos para procesar. -> {config.ambiente}");
                                WriteToLog(archivolog, $"No hay archivos para procesar.  -> {config.ambiente}");
                            }

                            // Procesar los archivos cargados
                            foreach (string filePath in filePaths)
                            {
                                // Console.WriteLine($"Archivo encontrado: {filePath}");
                                // Aquí realiza la logica de subir los archivos a un servidor SFTP

                                string fileName = Path.GetFileName(filePath);
                                //string directorio_name = Path.GetDirectoryName(filePath);
                                //string localFilePath = string.Concat(directorio_name, fileName); // Ruta local del archivo que deseas subir
                                string remoteFilePath = string.Concat(config.remoteFilePath, fileName); // Ruta remota donde se almacenará el archivo en el servidor SFTP

                                try
                                {
                                    if (string.IsNullOrEmpty(config.Password))
                                    {
                                        // instanciamos la clase
                                        PrivateKeyFile privateKeyFile = new PrivateKeyFile(config.privateKeyFilePath);

                                        var connectionInfo = new ConnectionInfo(config.Host, config.Port, config.Username, new PrivateKeyAuthenticationMethod(config.Username, new PrivateKeyFile[] { privateKeyFile }));

                                        // Crear la conexión SFTP
                                        using (var client = new SftpClient(connectionInfo))
                                        {
                                            client.Connect(); // Conectar al servidor
                                            WriteToLog(archivolog, $"Conexión con privateKey. -> {config.ambiente}");

                                            // Verificar si la conexión se estableció con éxito
                                            if (client.IsConnected)
                                            {
                                                Console.WriteLine($"Conexión SFTP establecida. -> {config.ambiente}");

                                                using (var fileStream = File.OpenRead(filePath)) // Abrir el archivo local para subirlo
                                                {
                                                    client.UploadFile(fileStream, remoteFilePath); // Subir el archivo al servidor SFTP
                                                    Console.WriteLine($"Archivo subido con éxito: {filePath}"); // revision en consola del archivo que se esta cargando
                                                    WriteToLog(archivolog, $"Archivo subido con éxito: {filePath}");
                                                }

                                                string destinationFilePath = Path.Combine(destinationDirectory, fileName);

                                                try
                                                {
                                                    File.Move(filePath, destinationFilePath);
                                                    Console.WriteLine($"Archivo {config.filePaths}: {fileName} movido a: {destinationFilePath}");
                                                    WriteToLog(archivolog, $"Archivo {config.filePaths}: {fileName} movido a: {destinationFilePath}");
                                                }
                                                catch (IOException ex)
                                                {
                                                    WriteToLog(archivolog, $"Error al mover el archivo: {ex.Message}");
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("No se pudo establecer la conexión SFTP."); // mensaje de error que no se puedo concectar al SFTP
                                                WriteToLog(archivolog, "No se pudo establecer la conexión SFTP.");
                                            }

                                            client.Disconnect(); // Cerrar la conexión SFTP
                                        }
                                    }
                                    else
                                    {
                                        // Crear la conexión SFTP
                                        using (var client = new SftpClient(config.Host, config.Port, config.Username, config.Password))
                                        {
                                            client.Connect(); // Conectar al servidor
                                            WriteToLog(archivolog, $"Conexión con Password. -> {config.ambiente}");

                                            // Verificar si la conexión se estableció con éxito
                                            if (client.IsConnected)
                                            {
                                                Console.WriteLine($"Conexión SFTP establecida. -> {config.ambiente}");

                                                using (var fileStream = File.OpenRead(filePath)) // Abrir el archivo local para subirlo
                                                {
                                                    client.UploadFile(fileStream, remoteFilePath); // Subir el archivo al servidor SFTP
                                                    Console.WriteLine($"Archivo subido con éxito: {filePath}"); // revision en consola del archivo que se esta cargando
                                                    WriteToLog(archivolog, $"Archivo subido con éxito: {filePath}");
                                                }

                                                string destinationFilePath = Path.Combine(destinationDirectory, fileName);

                                                try
                                                {
                                                    File.Move(filePath, destinationFilePath);
                                                    Console.WriteLine($"Archivo  {config.filePaths} : {fileName} movido a: {destinationFilePath}");
                                                    WriteToLog(archivolog, $"Archivo  {config.filePaths} : {fileName} movido a: {destinationFilePath}");
                                                }
                                                catch (IOException ex)
                                                {
                                                    WriteToLog(archivolog, $"Error al mover el archivo: {ex.Message}");
                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine("No se pudo establecer la conexión SFTP."); // mensaje de error que no se puedo concectar al SFTP
                                                WriteToLog(archivolog, "No se pudo establecer la conexión SFTP.");
                                            }

                                            client.Disconnect(); // Cerrar la conexión SFTP
                                        }
                                    }

                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error: " + ex.Message);
                                    WriteToLog(archivolog, $"Error: {ex.Message}");
                                }

                            }

                        }
                        else
                        {
                            Console.WriteLine("Error: No se pudieron obtener todos los valores del archivo XML.");
                            WriteToLog(archivolog, "Error: No se pudieron obtener todos los valores del archivo XML.");
                        }
                    }
                    else
                    {
                        WriteToLog(log_service_all, "Error no se definio el ambiente: PRD | QAS | DEV, campo del XML <ambiente> no puede estar vacío");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error al leer el archivo XML: " + ex.Message);
                    WriteToLog(log_service_all, $"Error al leer el archivo XML: " + ex.Message);
                }
                finally
                {
                    isRunning = false;
                }
            }
            else
            {
                Console.WriteLine("Otra instancia ya está en progreso.");
                WriteToLog(log_service_all, "Otra instancia ya está en progreso.");
            }
        }

        private void WriteToLog(string filePath, string message)
        {
            using (StreamWriter writer = File.AppendText(filePath))
            {
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}

![visual](https://img.shields.io/badge/Visual_Studio-2022-blue)
![microsoft](https://img.shields.io/badge/Microsoft-yellowgreen)
![windows10](https://img.shields.io/badge/windows_10-success)
![windows11](https://img.shields.io/badge/windows_11-success)
![windowsServer1](https://img.shields.io/badge/windows_server_2012-success)
![windowsServer2](https://img.shields.io/badge/windows_server_2016-success)
![windowsServer3](https://img.shields.io/badge/windows_server_2019-success)
![OpenSSH](https://img.shields.io/badge/OpenSSH-important)
![xml](https://img.shields.io/badge/XML-important)
![net](https://img.shields.io/badge/.NET_Framework-v4.7-important)
![ssh](https://img.shields.io/badge/SSH.NET-2020.0.2-important)

# serviceSFTP (v1.0)

Sincronizador para subir archivos a un SFTP, se tiene el servicio compilado en C#, se debe crear como servicio en Windows para que pueda ejecutarse en segundo plano.

## Requisitos previos

* Tener instalador Visual Studio 2022 en la que fue desarrollado el proyecto.

## Instalación

La ruta recomendada que se usa en el proyecto es la siguiente: **C:\SFTPservice**

1.	Compilar el proyecto en C#, luego copiar los archivos que se han creado en la siguiente ruta:

  	serviceSFTP\bin\Debug\
  	
    copiar todos los archivos y guardarlos en alguna unidad de preferencia que sea en la unidad:

  	**C:\SFTPservice\app\”pegas_los_archivos”**

2.	Cuando ya tengas los archivos copiados en la ruta recomendada, se debe guardar dentro el siguiente archivo root.xml (este es el archivo configurable):

    ![Alt text](img/image.png)
  	
  	El archivo root.xml, lo adjuntare aquí como archivo para que lo puedan descargar.
  	
    Click aqui -> [root.xml](archivos/root.xml)

  	También dejare el contenido que deben copiar si en caso no puedan descargar el archivo, solo deben guardar el contenido con el mismo nombre: root.xml en la misma ruta descrita.

    ![img-estructura-xml-serviceSFTP](img/image-1.png)

    Donde:
  	
    **host ->** ip o dominio del SFTP.  
    **port ->** Puerto asignado a SFTP, por lo general es el 22.  
    **username ->** usuario SFTP, de preferencia con privilegios altos.  
    **password ->** contraseña del user SFTP.  
    **directoryPath ->** Directorio en donde crearan los archivos a subir al SFTP.  
    **filePaths ->** indicar el tipo de archivo a cargar.  
    **NamedestinationDirectory ->** Nombre del directorio donde se moverán los archivos ya subidos al SFTP.  
    **remoteFilePath ->** Directorio del SFTP donde se cargarán los archivos.  
  	**privateKeyFilePath ->** Ruta del archivos donde se encuentra el privateKey (.ssh no .ppk).  
    **logFilePath ->** Ruta en donde se guardaran los archivos log.  
	**ambiente ->** Este puede ser QAS | DEV | TEST | PRD, recordar que el nombre se usara como ruta de directorio para almacenar los archivos, este se complementa con **directoryPath**. Ejemplo: C:\upload\QAS o C:\upload\PRD, dentro de esas carpetas se deben almacenar los archivo a cargar.  

    Recordar:
  	
    Si tiene un archivo ppk -> putty, este no lo reconoce la libreria de SSH.NET, se necesita convertirlo, siga los siguientes pasos:
    
      I. Instale putty en su equipo.  
     II. Luego cargue el archivo clave (.ppt) en puttygen.exe.  
    III. Conversiones > Exportar clave OpenSSH (no la opción "forzar nuevo formato de archivo").  
     IV. Esto creara un archivo que comienza con:  
     ```
    -----BEGIN RSA PRIVATE KEY-----  
    MIIBOgIBAAJBAKj34GkxFhD90vcNLYLInFEX6Ppy1tPf9Cnzj4p4WGeKLs1Pt8Qu  
    KUpRKfFLfRYC9AIKjbJTWit+CqvjWYzvQwECAwEAAQJAIJLixBy2qpFoS4DSmoEm  
    o3qGy0t6z09AIJtH+5OeRV1be+N4cDYJKffGzDa88vQENZiRm0GRq6a+HPGQMd2k  
    TQIhAKMSvzIBnni7ot/OSie2TmJLY4SwTQAevXysE2RbFDYdAiEBCUEaRQnMnbp7  
    9mxDXDf6AU0cN/RPBjb9qSHDcWZHGzUCIG2Es59z8ugGrDY+pxLQnwfotadxd+Uy  
    v/Ow5T0q5gIJAiEAyS4RaI9YG8EWx/2w0T67ZUVAw8eOMB6BIUg0Xcu+3okCIBOs  
    /5OiPgoTdSy7bcF9IGpSE8ZgGKzgYQVZeN97YE00  
    -----END RSA PRIVATE KEY-----
    
    Esto debe funcionar 👍.  
3.	Luego de haber guardado los archivos en el paso uno, se debe crear el servicio con los siguientes comandos en Windows:

    Abrir CMD.exe en modo administrador y ejecutar lo siguiente:

    **Primer paso:**  
    **sc.exe create SFTPservice binpath= C:\SFTPservice\app\serviceSFTP.exe start= auto**
    
    Donde:  
    **SFTPservice ->** nombre que tendrá tu servicio, lo podrás ver en services.msc de Windows con el mismo nombre.  
    **C:\SFTPservice\app\ ->** ruta creada en el punto 1, donde se copiaron los archivos de compilación del proyecto de C#.  
    **start= auto ->** sentencia de comandos para indicar que este se inicie de manera automática al iniciar o reiniciar Windows.  
    
    Al ejecutar debería tener el siguiente resultado:
  	
    ![img-crear-service](img/image-2.png)

    **Segundo paso:**  
    Iniciar el servicio creado por CMD.exe, se debe ejecutar el siguiente comando:
    
    **net start SFTPservice**
    
    Donde:  
    **SFTPservice ->** Es el nombre que se asignó al servicio en el paso 3.
    
    Al ejecutar debería tener el siguiente resultado:

  	![img-iniciar-service](img/image-3.png)

    **Tercer paso:**
    Para detener el servicio por CMD.exe, se debe ejecutar el siguiente comando.
    
    **net stop SFTPservice**
    
    Donde:  
    **SFTPservice ->** Es el nombre que se asignó al servicio en el paso 3.  
    
    De igual forma se puede ingresar a los servicios de Windows para iniciar o detenerlos de forma manual.

4.	Para eliminar el servicio de Windows, ejecutar el siguiente comando en CMD modo administrador:

    **sc.exe delete SFTPservice**
    
    Donde:  
    **SFTPservice ->** nombre que tiene tu servicio, en caso sea otro el nombre cambiarlo.
  	
* Si todos los pasos enteriores se ejecutan de manera correcta, no debería haber problemas.

## Uso

Su uso puede servir como cron para poder subir archivos de manera masiva usando SFTP.

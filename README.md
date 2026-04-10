# 🔒 HyperVision Checker

<div align="center">

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![License](https://img.shields.io/badge/License-MIT-yellow)

**Herramienta todo-en-uno para analizar y gestionar las protecciones de seguridad de Windows, con diagnóstico de CPU y soporte para el método HyperVision Denuvo**

</div>

---

## 📖 ¿Qué es HyperVision Checker?

**HyperVision Checker** es una aplicación de escritorio para Windows que te permite:

- **Analizar** el estado de las protecciones de seguridad basadas en virtualización (VBS, HVCI, Credential Guard, etc.).
- **Desactivar** todas estas protecciones con un solo clic (necesario para ejecutar ciertos software o juegos que requieren controladores sin firma).
- **Activar** nuevamente la máxima seguridad cuando hayas terminado.
- **Revertir** cualquier cambio a la configuración anterior gracias a un sistema de backup automático.
- **Diagnosticar** tu CPU y saber si es compatible con el método HyperVision, mostrando riesgos y soluciones específicas.
- **Consultar** una tabla completa de compatibilidad (procesadores antiguos y modernos, Windows 10/11).

Todo ello con una **interfaz moderna, oscura y fácil de usar**.

---

## 🚀 Características principales

| Función | Descripción |
|---------|-------------|
| 🔍 **Verificar Sistema** | Analiza en tiempo real el estado de VBS, HVCI, Secure Boot, TPM, BitLocker, virtualización, etc. |
| ⚡ **Verificar Bypass** | Muestra los pasos exactos del método HyperVision y si tu sistema es compatible. |
| 📋 **Tabla de Compatibilidad** | Tabla interactiva con riesgos y soluciones para CPUs Intel y AMD (antiguas y modernas) y versiones de Windows. |
| ⚡ **Desactivar Todo** | Desactiva VBS, HVCI, Windows Hello, Credential Guard, KVA Shadow, Hypervisor y más. Guarda backup automático y ofrece reinicio con F7. |
| 🔒 **Activar Todo** | Restaura todas las protecciones al máximo nivel de seguridad. |
| 🔄 **Revertir Cambios** | Vuelve exactamente al estado anterior (usando el backup guardado). |
| 💾 **Guardar Backup** | Permite guardar manualmente un punto de restauración. |
| 🔁 **Reinicio F7** | Configura el arranque en modo avanzado y reinicia para que puedas presionar F7 y deshabilitar la firma de controladores. |
| 🖥️ **Diagnóstico avanzado de CPU** | Detecta generación, soporte MBEC, vulnerabilidades (Spectre, Meltdown, Zenbleed) y ofrece recomendaciones personalizadas. |

---

## 🖼️ Captura de pantalla

<img width="1183" height="1195" alt="image" src="https://github.com/user-attachments/assets/4dfc135c-8515-4400-94ea-78d4e9fce99e" />

*La interfaz muestra estadísticas en tiempo real y un área de resultados con formato de tabla.*

---

## 📋 Requisitos del sistema

| Requisito | Detalle |
|-----------|---------|
| **Sistema operativo** | Windows 10 / Windows 11 (64 bits) |
| **Arquitectura** | x64 |
| **Permisos** | Administrador (obligatorio) |
| **.NET Runtime** | No necesario si usas la versión autónoma (incluida en el .exe) |
| **Hardware** | CPU con soporte de virtualización (Intel VT-x / AMD-V) para poder modificar VBS/HVCI |

---

## 📦 Instalación y uso

### Opción A: Descargar ejecutable (recomendado)

1. Descarga la última versión desde la sección [Releases](https://github.com/tu-usuario/HyperVision-Checker/releases).
2. Extrae el contenido de `HyperVision_Checker.zip`.
3. Ejecuta `BypassCheckerWPF.exe` **como administrador** (clic derecho → "Ejecutar como administrador").

### Opción B: Compilar desde código fuente

```bash
git clone https://github.com/tu-usuario/HyperVision-Checker.git
cd HyperVision-Checker
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:DebugType=none -p:EnableCompressionInSingleFile=true -o ./publish-single

🎮 Guía rápida de uso
Ejecuta como administrador (necesario para leer/modificar configuraciones del sistema).

Haz clic en "Verificar Sistema" para ver el estado actual.

Si deseas desactivar las protecciones (por ejemplo, para jugar o usar software que requiere controladores sin firma), haz clic en "Desactivar Todo".

El programa guardará automáticamente un backup de tu configuración.

Te preguntará si quieres reiniciar. Si aceptas, el sistema se reiniciará en modo avanzado.

En el arranque, presiona F7 para deshabilitar la firma obligatoria de controladores.

Cuando hayas terminado, abre el programa nuevamente y haz clic en "Revertir Cambios" para restaurar tu configuración original.

También puedes usar "Activar Todo" si prefieres volver al máximo nivel de seguridad (sin respetar el estado anterior).

⚠️ Advertencias importantes
Desactivar estas protecciones reduce significativamente la seguridad de tu sistema. Solo hazlo en entornos controlados y desconectado de Internet.

El modo F7 es temporal (solo dura una sesión de arranque). Al reiniciar normalmente, las protecciones volverán a estar activas (a menos que las hayas desactivado permanentemente con el botón "Desactivar Todo").

BitLocker se suspenderá automáticamente durante el proceso (si está activo). Necesitarás tu clave de recuperación si algo sale mal.

Anti-cheats como EAC, BattlEye, Vanguard o FACEIT pueden bloquear el método. Se recomienda desinstalarlos antes de usar la desactivación.

Algunos CPUs (especialmente Intel 12ª Gen con E-cores, Ryzen 7000/8000) pueden presentar inestabilidad o BSOD. El programa te advertirá con el diagnóstico de CPU.

🧠 Diagnóstico de CPU
El programa analiza tu procesador y te muestra:

Nivel de riesgo (Alto, Medio, Bajo) según la generación y modelo.

Soporte MBEC (rendimiento optimizado con VBS).

Vulnerabilidades conocidas (Spectre, Meltdown, Zenbleed).

Soluciones recomendadas (actualizar BIOS, desactivar E-cores, forzar afinidad, etc.).

Tabla de compatibilidad integrada (accesible desde el botón "📋 Tabla de Compatibilidad") cubre desde Intel Core 2 hasta las últimas generaciones (Ultra 200, Ryzen 9000) y Windows 10/11.

🛠️ Tecnologías utilizadas
.NET 8 y WPF para la interfaz de usuario.

WMI (System.Management) para consultar hardware (CPU, TPM, BitLocker, etc.).

Registro de Windows y reg.exe para modificar las claves de seguridad.

bcdedit para gestionar el hipervisor y el arranque avanzado.

Process.Start para ejecutar comandos con privilegios elevados.

📂 Estructura del proyecto
text
HyperVision-Checker/
├── BypassCheckerWPF.csproj      # Archivo de proyecto
├── app.manifest                 # Manifiesto para solicitar administrador
├── App.xaml / App.xaml.cs       # Configuración de la aplicación
├── MainWindow.xaml              # Interfaz de usuario
├── MainWindow.xaml.cs           # Lógica de eventos y UI
├── SystemChecker.cs             # Toda la lógica de verificación y acciones
├── CheckResults.cs              # Modelo de resultados
├── BypassResults.cs             # Modelo de bypass
├── HardwareSecurity.cs          # Información avanzada del CPU
└── README.md                    # Este archivo
🤝 Contribuciones
Las contribuciones son bienvenidas. Si encuentras un error o quieres mejorar alguna funcionalidad:

Haz un fork del repositorio.

Crea una rama (git checkout -b feature/nueva-funcionalidad).

Realiza tus cambios y haz commit (git commit -m 'Agrega nueva funcionalidad').

Sube la rama (git push origin feature/nueva-funcionalidad).

Abre un Pull Request.

📄 Licencia
Este proyecto está bajo la licencia MIT. Ver el archivo LICENSE para más detalles.

🙏 Agradecimientos
modgames44 por el script original HyperVisor-Denuvo que inspiró la gestión de características.

A toda la comunidad de cs.rin.ru y Reddit por reportar problemas y soluciones en los diferentes procesadores y versiones de Windows.



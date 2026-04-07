# 🔒 HyperVision Checker

<div align="center">

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

**Herramienta de análisis de compatibilidad para el método HyperVision Denuvo**

[Características](#-características) •
[Requisitos](#-requisitos-del-sistema) •
[Instalación](#-instalación) •
[Uso](#-uso) •
[Compilación](#-compilación-desde-código-fuente)

</div>

---

## 📖 ¿Qué es HyperVision Checker?

**HyperVision Checker** es una aplicación de escritorio para Windows que analiza tu sistema y determina si es **compatible con el método HyperVision** para desactivar protecciones basadas en virtualización (VBS/HVCI).

La herramienta verifica en tiempo real el estado de:
- Virtualización del procesador (VT-x/AMD-V)
- VBS (Virtualization-Based Security)
- HVCI (Memory Integrity)
- UEFI Lock
- BitLocker
- TPM
- Secure Boot

---

## ✨ Características

| Característica | Descripción |
|----------------|-------------|
| 🔍 **Análisis completo** | Verifica todos los componentes necesarios para el método HyperVision |
| 🎨 **Interfaz moderna** | Diseño oscuro con estadísticas en tiempo real |
| ⚡ **Sin base de datos** | Consulta directamente al hardware y sistema operativo |
| 📊 **Estadísticas visuales** | Panel con estado actual de virtualización y compatibilidad |
| 🚀 **Método HyperVision** | Pasos detallados basados en el repositorio original |
| 💾 **Portable** | No requiere instalación, un solo ejecutable |

---

## 📋 Requisitos del Sistema

### Mínimos:
| Componente | Requisito |
|------------|-----------|
| **Sistema Operativo** | Windows 10 / Windows 11 (64 bits) |
| **Arquitectura** | x64 |
| **.NET Runtime** | .NET 8.0 (solo para versión no autónoma) |
| **Permisos** | Administrador (requerido) |

### Recomendados para el método HyperVision:
| Componente | Estado requerido |
|------------|------------------|
| **Virtualización** | ✅ Activada en BIOS (VT-x/AMD-V) |
| **VBS/HVCI** | ⚠️ Puede estar activado o desactivado |
| **BitLocker** | ⚠️ Se suspenderá automáticamente |
| **UEFI Lock** | ⚠️ Puede impedir la desactivación |

---

## 🚀 Instalación

### Opción A: Descargar ejecutable (Recomendado)
1. Descarga el archivo `HyperVision_Checker.zip` desde [Releases]([https://github.com/tu-usuario/HyperVision-Checker/releases](https://github.com/modgames44/HiperVisor-Checker/releases))
2. Extrae el contenido en una carpeta
3. Ejecuta `BypassCheckerWPF.exe` **como Administrador**

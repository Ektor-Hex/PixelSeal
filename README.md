# 🔒 PixelSeal

**Secure Image Redaction Tool** — Aplicación de escritorio profesional para redactar información sensible en imágenes de forma **irreversible**.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Desktop-0078D4?style=flat-square&logo=windows)
![SkiaSharp](https://img.shields.io/badge/SkiaSharp-2.88.7-green?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)

---

## 📋 Descripción

PixelSeal es una herramienta de redacción de imágenes diseñada con seguridad como prioridad. A diferencia de otras herramientas que usan blur o pixelación (que pueden ser reversibles), PixelSeal **destruye completamente los píxeles originales** y los reemplaza con contenido sintético.

### ⚠️ Principios de Seguridad

- ❌ **NO blur** — El blur es reversible con técnicas de deconvolución
- ❌ **NO pixelación** — Puede ser reconstruida con IA
- ❌ **NO alpha blending** — Los datos originales permanecen en el canal
- ✅ **100% destrucción de píxeles** — Reemplazo completo e irreversible
- ✅ **Sin metadatos EXIF** — Eliminación automática al cargar
- ✅ **Re-codificación completa** — La imagen exportada es completamente nueva

---

## ✨ Características

### 🎨 Modos de Redacción

| Modo | Descripción |
|------|-------------|
| **■ Solid Overwrite** | Relleno sólido 100% opaco con color configurable |
| **📝 Semantic Placeholder** | Panel elegante con texto (REDACTED, CONFIDENTIAL, HIDDEN) |
| **⬡ Geometric Pattern** | Patrones sintéticos (líneas, cuadrícula, puntos) |
| **🏷️ Context-Aware Panel** | Tarjeta estilo UI con sombra e icono |
| **✨ Glass Morph** | Efecto cristal esmerilado estilo Twitter/X stickers |

### 🖌️ Formas de Región

- **▭ Rectángulo** — Selección rectangular estándar
- **⬭ Elipse** — Selección circular/elíptica
- **✏️ Pincel Libre** — Dibujo a mano alzada con tamaño configurable

### 🛠️ Funcionalidades

- ✅ Carga de imágenes (PNG, JPG, JPEG, BMP, WebP)
- ✅ Múltiples regiones por imagen
- ✅ Vista previa antes de exportar
- ✅ Exportación segura (PNG/JPG)
- ✅ Zoom y navegación
- ✅ Selección y edición de regiones
- ✅ Interfaz oscura profesional
- ✅ Atajos de teclado

---

## 🏗️ Arquitectura

El proyecto sigue una arquitectura de 4 capas con separación clara de responsabilidades:

```
PixelSeal/
├── PixelSeal.Models/        # Modelos de dominio (sin dependencias)
├── PixelSeal.Engine/        # Motor de redacción (SkiaSharp, sin WPF)
├── PixelSeal.Infrastructure/ # Servicios (carga, exportación, orquestación)
└── PixelSeal.UI/            # Interfaz WPF (MVVM)
```

### Dependencias

- **SkiaSharp 2.88.7** — Renderizado de gráficos multiplataforma
- **CommunityToolkit.Mvvm 8.2.2** — Framework MVVM

---

## 🚀 Instalación

### Requisitos

- Windows 10/11
- .NET 8.0 SDK

### Compilar desde código fuente

```bash
# Clonar el repositorio
git clone https://github.com/tu-usuario/PixelSeal.git
cd PixelSeal

# Restaurar dependencias y compilar
dotnet restore
dotnet build

# Ejecutar
dotnet run --project PixelSeal.UI
```

### Publicar ejecutable

```bash
dotnet publish PixelSeal.UI -c Release -r win-x64 --self-contained
```

---

## 📖 Uso

### Flujo de trabajo básico

1. **Abrir imagen** — `Ctrl+O` o botón "📂 Open Image"
2. **Seleccionar área** — Dibuja directamente en la imagen o usa "➕ Add Region"
3. **Elegir modo** — Selecciona el tipo de redacción deseado
4. **Configurar opciones** — Ajusta colores, bordes, patrones, etc.
5. **Vista previa** — Botón "✅ Apply Redaction" para previsualizar
6. **Exportar** — `Ctrl+S` o botón "💾 Export"

### Atajos de teclado

| Atajo | Acción |
|-------|--------|
| `Ctrl+O` | Abrir imagen |
| `Ctrl+S` | Exportar imagen |
| `Delete` | Eliminar región seleccionada |
| `Escape` | Cancelar dibujo |
| `Shift+Drag` | Agregar nueva región (cuando hay regiones existentes) |

---

## 🔧 Configuración de modos

### Solid Overwrite
- Color de relleno (hex)
- Radio de esquinas (0-6px)
- Color y grosor de borde

### Semantic Placeholder
- Texto: REDACTED, CONFIDENTIAL, HIDDEN
- Color de fondo
- Color de texto
- Radio de esquinas

### Geometric Pattern
- Tipo: Líneas, Cuadrícula, Puntos
- Color del patrón
- Color de fondo
- Densidad del patrón

### Context-Aware Panel
- Color de fondo del panel
- Color del borde
- Radio de esquinas
- Desplazamiento de sombra
- Mostrar/ocultar icono

### Glass Morph
- Color del tinte (hex)
- Radio de esquinas (0-20px)
- Color y grosor de borde opcional

---

## 📁 Estructura del proyecto

```
PixelSeal/
├── PixelSeal.sln
├── README.md
├── PixelSeal.Models/
│   ├── RedactionMode.cs
│   ├── RedactionOptions.cs
│   ├── RedactionRegion.cs
│   ├── RegionShape.cs
│   ├── PatternType.cs
│   ├── PlaceholderText.cs
│   └── ExportFormat.cs
├── PixelSeal.Engine/
│   ├── RedactionEngine.cs
│   ├── RedactionStrategyFactory.cs
│   ├── IRedactionStrategy.cs
│   ├── ColorParser.cs
│   └── Strategies/
│       ├── SolidOverwriteStrategy.cs
│       ├── SemanticPlaceholderStrategy.cs
│       ├── GeometricPatternStrategy.cs
│       ├── ContextAwarePanelStrategy.cs
│       └── GlassMorphStrategy.cs
├── PixelSeal.Infrastructure/
│   ├── ImageLoader.cs
│   ├── SecureImageExporter.cs
│   └── RedactionService.cs
└── PixelSeal.UI/
    ├── App.xaml
    ├── MainWindow.xaml
    ├── Controls/
    │   ├── ImageCanvas.xaml
    │   └── ModeOptionsPanel.xaml
    ├── ViewModels/
    │   ├── MainViewModel.cs
    │   └── RegionViewModel.cs
    ├── Converters/
    │   └── Converters.cs
    └── Themes/
        ├── Colors.xaml
        └── Styles.xaml
```

---

## 🔒 Seguridad

### Proceso de exportación seguro

1. **Carga** — Los metadatos EXIF se eliminan automáticamente
2. **Redacción** — Los píxeles originales son completamente destruidos
3. **Aplanamiento** — Todas las capas se combinan en una sola
4. **Re-codificación** — La imagen se codifica completamente desde cero
5. **Sin alpha** — El canal alpha se elimina (formato opaco)

### Garantías

- Los píxeles redactados **NO pueden ser recuperados**
- No hay capas ocultas ni datos residuales
- La imagen exportada es completamente nueva

---

## 🤝 Contribuir

Las contribuciones son bienvenidas. Por favor:

1. Fork el repositorio
2. Crea una rama para tu feature (`git checkout -b feature/nueva-funcionalidad`)
3. Commit tus cambios (`git commit -m 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Abre un Pull Request

---

## 📄 Licencia

Este proyecto está bajo la Licencia MIT. Ver el archivo [LICENSE](LICENSE) para más detalles.

---

## 👨‍💻 Créditos

**Desarrollado por [Ektor](https://github.com/Ektor-Hex)**

---

## 📞 Contacto

Para reportar bugs, sugerencias o preguntas, por favor abre un [Issue](https://github.com/Ektor-Hex/PixelSeal/issues) en el repositorio.

---

<p align="center">
  <b>🔒 PixelSeal</b> — Porque la privacidad importa.
</p>

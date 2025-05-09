# dotnetfuzz

**dotnetfuzz** Es una herramienta desarrollada en C# para realizar Fuzzing de directorios y archivos en aplicaciones Web. la cual permite detectar recursos accesibles mediante solicitudes HTTP.


## Uso

```bash
./dotnetfuzz -u <url> -w <wordlists> [opciones]

./dotnetfuzz - -u https://misitio.com -w wordlist.txt -x php,txt -t 20 --timeout 8 -o resultados.txt
```

## Opciones


| Opción        | Descripción                       |
|---------------|-----------------------------------|
| -h            | Mostrar ayuda  |
| -u            | URl objetivo   |
| -w            | Ruta del diccionario (wordlists)  |
| -x            | Lista de extensiones separadas por coma (ej: php,html,txt) |
| -t            | Número de tareas en paralelo (por defecto: 10) |
| --timeout     | Tiempo de espera por solicitud en segundos (por defecto: 5) |
| -o            | Archivo donde guardar los resultados válidos |


## Ejemplo de Salida

```
index               Status: 200
robots.txt          Status: 200
login               Status: 301
admin               Status: 403

[##########--------------------] 40.5% (40512/100000)
```


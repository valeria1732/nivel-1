@echo off
title Servidor Permanente - Madres Buscadoras
cd /d "c:\Users\calvi\Downloads\Madres buscadoras\ItchIo_Web"
start /b python -m http.server 8080 --directory "c:\Users\calvi\Downloads\Madres buscadoras\ItchIo_Web"
:loop
ssh -o StrictHostKeyChecking=no -o ServerAliveInterval=20 -R 80:127.0.0.1:8080 nokey@localhost.run
timeout /t 3 >nul
goto loop


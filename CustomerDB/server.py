"""
CustomerDB Proxy Server - port 8600
Serves static files AND proxies /api/* to the ASP.NET API on port 5000.
Run this instead of 'python -m http.server 8600'
"""
import http.server
import urllib.request
import urllib.error
import os
import sys

PORT = 8600
API_PORT = 5000
STATIC_DIR = os.path.dirname(os.path.abspath(__file__))

class ProxyHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=STATIC_DIR, **kwargs)

    def do_GET(self):
        if self.path.startswith('/api/'):
            self.proxy_request('GET', None)
        else:
            super().do_GET()

    def do_POST(self):
        if self.path.startswith('/api/'):
            length = int(self.headers.get('Content-Length', 0))
            body = self.rfile.read(length) if length else None
            self.proxy_request('POST', body)
        else:
            super().do_POST()

    def do_PATCH(self):
        if self.path.startswith('/api/'):
            length = int(self.headers.get('Content-Length', 0))
            body = self.rfile.read(length) if length else None
            self.proxy_request('PATCH', body)

    def do_DELETE(self):
        if self.path.startswith('/api/'):
            self.proxy_request('DELETE', None)

    def proxy_request(self, method, body):
        target = f'http://localhost:{API_PORT}{self.path}'
        try:
            req = urllib.request.Request(target, data=body, method=method)
            req.add_header('Content-Type', self.headers.get('Content-Type', 'application/json'))
            with urllib.request.urlopen(req) as resp:
                data = resp.read()
                self.send_response(resp.status)
                self.send_header('Content-Type', resp.headers.get('Content-Type', 'application/json'))
                self.send_header('Content-Length', len(data))
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(data)
        except urllib.error.HTTPError as e:
            data = e.read()
            self.send_response(e.code)
            self.send_header('Content-Type', 'application/json')
            self.send_header('Content-Length', len(data))
            self.end_headers()
            self.wfile.write(data)
        except Exception as e:
            self.send_response(502)
            msg = str(e).encode()
            self.send_header('Content-Length', len(msg))
            self.end_headers()
            self.wfile.write(msg)

    def do_OPTIONS(self):
        self.send_response(200)
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, POST, PATCH, DELETE, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', 'Content-Type')
        self.end_headers()

    def log_message(self, format, *args):
        print(f"[{self.address_string()}] {format % args}")

if __name__ == '__main__':
    os.chdir(STATIC_DIR)
    server = http.server.HTTPServer(('0.0.0.0', PORT), ProxyHandler)
    print(f"CustomerDB Proxy Server running on port {PORT}")
    print(f"  Static files: {STATIC_DIR}")
    print(f"  API proxy: localhost:{API_PORT} -> :{PORT}/api/")
    print(f"  Admin:  http://192.168.1.35:{PORT}/admin.html")
    print(f"  MedRep: http://192.168.1.35:{PORT}/medrep.html")
    server.serve_forever()

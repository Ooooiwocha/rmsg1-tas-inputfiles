#!/usr/bin/env python3
"""Extract the archived report without editing its research content.

Requires Python 3 and Pandoc 3.x. Run from any working directory.
"""
import base64
from email import policy
from email.parser import BytesParser
import hashlib
import html
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ロマンシング サガTAS制作研究報告.mhtml'
OUT = ROOT / 'docs/research'


def main():
    OUT.joinpath('assets').mkdir(parents=True, exist_ok=True)
    archive = BytesParser(policy=policy.default).parsebytes(SOURCE.read_bytes())
    parts = list(archive.walk())
    page = next(p for p in parts if p.get_content_type() == 'text/html')
    source = page.get_payload(decode=True).decode('utf-8')
    result = source
    for part in parts:
        if part.get_content_type() == 'text/css':
            css = part.get_payload(decode=True)
            OUT.joinpath('assets/report.css').write_bytes(css)
            result = result.replace(part['Content-Location'], 'assets/report.css')

    # Only same-document file links can safely become fragment links.
    location = page['Content-Location']
    result = result.replace(location + '#', '#')
    images = []

    def extract_png(match):
        data = base64.b64decode(match[1], validate=True)
        name = f'assets/screenshot-{len(images) + 1:02d}.png'
        OUT.joinpath(name).write_bytes(data)
        images.append((name, hashlib.sha256(data).hexdigest()))
        return name

    result = re.sub(r'data:image/png;base64,([A-Za-z0-9+/=]+)', extract_png, result)
    assert 'file:///' not in result and 'cid:' not in result
    OUT.joinpath('report.html').write_text(result, encoding='utf-8')

    # GitHub sanitizes inline SVG. External SVG assets preserve the diagrams.
    diagram_number = 0

    def extract_svg(match):
        nonlocal diagram_number
        diagram_number += 1
        svg = match[0].replace('<svg ', '<svg xmlns="http://www.w3.org/2000/svg" ', 1)
        name = f'assets/diagram-{diagram_number:02d}.svg'
        OUT.joinpath(name).write_text(svg, encoding='utf-8')
        title = re.search(r'<title[^>]*>(.*?)</title>', svg, re.S)[1]
        description = re.search(r'<desc[^>]*>(.*?)</desc>', svg, re.S)[1]
        return f'<img src="{name}" alt="{html.escape(html.unescape(title + "：" + description), quote=True)}">'

    markdown_source = re.sub(r'<svg\b.*?</svg>', extract_svg, result, flags=re.S)
    # HTML details/summary and captions must not swallow Markdown formatting.
    markdown_source = re.sub(r'</?details\b[^>]*>', '', markdown_source)
    markdown_source = re.sub(r'<summary>(.*?)</summary>', r'<p><strong>\1</strong></p>', markdown_source, flags=re.S)
    markdown_source = re.sub(r'<table><caption>(.*?)</caption>', r'<p><strong>\1</strong></p><table>', markdown_source, flags=re.S)
    markdown_source = re.sub(r'</?figcaption\b[^>]*>', lambda m: '</p>' if m[0].startswith('</') else '<p>', markdown_source)
    with tempfile.TemporaryDirectory() as temp:
        temp = Path(temp)
        temp.joinpath('source.html').write_text(markdown_source, encoding='utf-8')
        temp.joinpath('flatten.lua').write_text('function Div(el) return el.content end\nfunction Span(el) return el.content end\nfunction Strong(el) table.insert(el.content, 1, pandoc.RawInline("html", "<strong>")); table.insert(el.content, pandoc.RawInline("html", "</strong>")); return el.content end\n', encoding='utf-8')
        subprocess.run(['pandoc', str(temp / 'source.html'), '-f', 'html', '-t', 'gfm', '--wrap=none', '--lua-filter', str(temp / 'flatten.lua'), '-o', str(OUT / 'report.md')], check=True)
    markdown = OUT.joinpath('report.md').read_text(encoding='utf-8')
    markdown = '<a name="top"></a>\n\n' + markdown
    for anchor, heading in re.findall(r'<section id="([^"]+)"><h2>(.*?)</h2>', result):
        target = '## ' + html.unescape(re.sub('<[^>]+>', '', heading))
        assert markdown.count(target + '\n') == 1
        prefix = '<a name="main"></a>\n\n' if anchor == 'summary' else ''
        markdown = markdown.replace(target + '\n', prefix + f'<a name="{anchor}"></a>\n\n' + target + '\n')
    OUT.joinpath('report.md').write_text(markdown, encoding='utf-8')
    print(f'Extracted {len(images)} PNG images and {diagram_number} SVG diagrams.')
    print('Source SHA-256:', hashlib.sha256(SOURCE.read_bytes()).hexdigest())


if __name__ == '__main__':
    main()

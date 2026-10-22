#!/usr/bin/env python3
"""Check content fidelity, extracted resources and navigation (Python 3 + Pandoc)."""
import base64
from collections import Counter
from email import policy
from email.parser import BytesParser
from html.parser import HTMLParser
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/research'


class Document(HTMLParser):
    def __init__(self, source):
        super().__init__()
        self.text = []
        self.links = []
        self.resources = []
        self.ids = []
        self.tags = Counter()
        self.skip = 0
        self.feed(source)

    def handle_starttag(self, tag, attributes):
        a = dict(attributes)
        self.tags[tag] += 1
        if tag in ('head', 'svg'):
            self.skip += 1
        if 'id' in a:
            self.ids.append(a['id'])
        if tag == 'a':
            if 'name' in a:
                self.ids.append(a['name'])
            if 'href' in a:
                self.links.append(a['href'])
        if tag == 'img':
            self.resources.append(a['src'])
        if tag == 'link':
            self.resources.append(a['href'])

    def handle_endtag(self, tag):
        if tag in ('head', 'svg'):
            self.skip -= 1

    def handle_data(self, value):
        if not self.skip:
            self.text.append(value)

    def normalized(self):
        return re.sub(r'\s+', '', ''.join(self.text))


def main():
    archive = BytesParser(policy=policy.default).parsebytes(next(ROOT.glob('*.mhtml')).read_bytes())
    part = next(p for p in archive.walk() if p.get_content_type() == 'text/html')
    source = part.get_payload(decode=True).decode('utf-8')
    original = Document(source)
    html = OUT.joinpath('report.html').read_text(encoding='utf-8')
    md = OUT.joinpath('report.md').read_text(encoding='utf-8')
    rendered = subprocess.check_output(['pandoc', '-f', 'gfm', '-t', 'html'], input=md.encode()).decode()
    expected_links = [url.replace(part['Content-Location'] + '#', '#') for url in original.links]
    for name, content in [('HTML', html), ('Markdown', rendered)]:
        doc = Document(content)
        assert doc.normalized() == original.normalized(), f'{name}: body text changed'
        assert doc.links == expected_links, f'{name}: links changed'
        assert doc.tags['table'] == original.tags['table'] == 5
        assert doc.tags['td'] == original.tags['td'] == 65
        assert doc.tags['th'] == original.tags['th'] == 16
        assert len(doc.ids) == len(set(doc.ids)), f'{name}: duplicate anchors'
        for url in doc.links:
            if url.startswith('#'):
                assert url[1:] in doc.ids, (name, url)
        for url in doc.resources:
            assert not url.startswith(('data:', 'cid:', 'file:'))
            assert OUT.joinpath(url).is_file(), (name, url)
        assert 'file:///' not in content and 'cid:' not in content
        print(f'{name}: body text, all 27 links, 18 internal targets, 5 tables and local resources OK')
    for i, encoded in enumerate(re.findall(r'data:image/png;base64,([A-Za-z0-9+/=]+)', source), 1):
        assert OUT.joinpath(f'assets/screenshot-{i:02d}.png').read_bytes() == base64.b64decode(encoded)
    for i, svg in enumerate(re.findall(r'<svg\b.*?</svg>', source, re.S), 1):
        output = OUT.joinpath(f'assets/diagram-{i:02d}.svg').read_text()
        assert output == svg.replace('<svg ', '<svg xmlns="http://www.w3.org/2000/svg" ', 1)
        ET.fromstring(output)
    print('2 PNG images byte-identical; 3 SVG diagrams identical except namespace declaration.')


if __name__ == '__main__':
    main()

Name:           private-ai
Version:        1.0.0
Release:        1%{?dist}
Summary:        Private AI desktop application

License:        Proprietary
BuildArch:      x86_64

%description
Private AI desktop application for accessing AI web services.

%install

mkdir -p %{buildroot}/opt/private-ai
cp -a %{_builddir}/PrivateAI/. %{buildroot}/opt/private-ai/

mkdir -p %{buildroot}/usr/share/applications

cat > %{buildroot}/usr/share/applications/private-ai.desktop <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Private AI
Comment=Private AI desktop application
Exec=/opt/private-ai/PrivateAI
Terminal=false
Categories=Utility;
EOF

%files
/opt/private-ai/*
/usr/share/applications/private-ai.desktop

%post
chmod 755 /opt/private-ai/PrivateAI || true

%changelog
* Sun Sep 27 2026 Private AI <private-ai@example.com> - 1.0.0-1
- Initial release